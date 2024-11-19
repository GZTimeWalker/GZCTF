import type { PowRequest, PowResult } from '@Components/HashPow'

const workerFunction = function () {
  class PowWorker {
    prefix: Uint8Array
    diff: number

    constructor(prefix: string, diff: number) {
      this.prefix = this.parsePrefix(prefix)
      this.diff = diff
    }

    parsePrefix(hex: string): Uint8Array {
      const bytes = new Uint8Array(hex.length / 2 + 4)
      for (let i = 0; i < hex.length; i += 2) {
        bytes[i / 2] = parseInt(hex.slice(i, i + 2), 16)
      }
      for (let i = hex.length / 2; i < hex.length / 2 + 4; i++) {
        bytes[i] = Math.floor(Math.random() * 256)
      }
      return bytes
    }

    getNonce(nonce: number): string {
      const random = this.toHex(this.prefix.slice(this.prefix.length - 4))
      return `${random}${nonce.toString(16).padStart(8, '0')}`
    }

    toHex(bytes: ArrayBuffer): string {
      return Array.from(new Uint8Array(bytes))
        .map((byte) => byte.toString(16).padStart(2, '0'))
        .join('')
    }

    leadingZeros(hash: Uint8Array): number {
      let count = 0
      for (let i = 0; i < hash.length; i++) {
        if (hash[i] === 0) {
          count += 8
        } else {
          let mask = 0x80
          for (let j = 0; j < 8; j++) {
            if ((hash[i] & mask) === 0) {
              count++
              mask >>= 1
            } else {
              break
            }
          }
          break
        }
      }
      return count
    }

    concatNonce(nonce: number): Uint8Array {
      const buffer = new Uint8Array(this.prefix.length + 4)
      buffer.set(this.prefix, 0)
      for (let i = 0; i < 4; i++) {
        buffer[this.prefix.length + i] = (nonce >> (24 - i * 8)) & 0xff
      }
      return buffer
    }

    async solve(): Promise<PowResult> {
      // return null if web crypto is not available
      if (!self.crypto || !self.crypto.subtle) {
        return { nonce: null, time: 0, rate: 0 }
      }

      let nonce = Math.floor(Math.random() * 0xffffffff)
      const originalNonce = nonce
      const startTime = Date.now()
      while (true) {
        const data = this.concatNonce(nonce)
        const hash = await crypto.subtle.digest('SHA-256', data)
        if (this.leadingZeros(new Uint8Array(hash)) >= this.diff) {
          break
        }
        nonce++
      }
      const time = Date.now() - startTime
      const trials = nonce - originalNonce

      return { nonce: this.getNonce(nonce), time, rate: trials / time }
    }
  }

  self.onmessage = (event: MessageEvent<PowRequest>) => {
    const { chall, diff } = event.data
    const worker = new PowWorker(chall, diff)
    worker.solve().then((nonce) => {
      self.postMessage(nonce)
    })
  }
}

const codeStr = workerFunction.toString()
const mainCode = codeStr.substring(codeStr.indexOf('{') + 1, codeStr.lastIndexOf('}'))
const blob = new Blob([mainCode], { type: 'application/javascript' })
const workerScript = URL.createObjectURL(blob)

export default workerScript
