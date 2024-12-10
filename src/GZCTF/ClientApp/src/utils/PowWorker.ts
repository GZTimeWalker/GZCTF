import type { PowRequest, PowResult } from '@Components/HashPow'

const workerFunction = function () {
  const solve = async (req: PowRequest): Promise<PowResult> => {
    // return null if web crypto is not available
    if (!self.crypto || !self.crypto.subtle) {
      return { nonce: null, time: 0, rate: 0 }
    }

    const prefix = parsePrefix(req.chall)
    const diff = req.diff

    let nonce = Math.floor(Math.random() * 0xffffffff)
    const originalNonce = nonce
    const startTime = Date.now()
    while (true) {
      const data = concatNonce(prefix, nonce)
      const hash = await crypto.subtle.digest('SHA-256', data)
      if (leadingZeros(new Uint8Array(hash)) >= diff) {
        break
      }
      nonce++
    }
    const time = Date.now() - startTime
    const trials = nonce - originalNonce

    return { nonce: getNonce(prefix, nonce), time, rate: trials / time }
  }

  const concatNonce = (prefix: Uint8Array, nonce: number): Uint8Array => {
    const buffer = new Uint8Array(prefix.length + 4)
    buffer.set(prefix, 0)
    for (let i = 0; i < 4; i++) {
      buffer[prefix.length + i] = (nonce >> (24 - i * 8)) & 0xff
    }
    return buffer
  }

  const parsePrefix = (hex: string): Uint8Array => {
    const bytes = new Uint8Array(hex.length / 2 + 4)
    for (let i = 0; i < hex.length; i += 2) {
      bytes[i / 2] = parseInt(hex.slice(i, i + 2), 16)
    }
    for (let i = hex.length / 2; i < hex.length / 2 + 4; i++) {
      bytes[i] = Math.floor(Math.random() * 256)
    }
    return bytes
  }

  const getNonce = (prefix: Uint8Array, nonce: number): string => {
    const random = toHex(prefix.slice(prefix.length - 4))
    return `${random}${nonce.toString(16).padStart(8, '0')}`
  }

  const toHex = (bytes: Uint8Array): string => {
    return Array.from(bytes)
      .map((byte) => byte.toString(16).padStart(2, '0'))
      .join('')
  }

  const leadingZeros = (hash: Uint8Array): number => {
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

  self.onmessage = async (event: MessageEvent<PowRequest>) => {
    const result = await solve(event.data)
    self.postMessage(result)
  }
}

const codeStr = workerFunction.toString()
const mainCode = codeStr.substring(codeStr.indexOf('{') + 1, codeStr.lastIndexOf('}'))
const blob = new Blob([mainCode], { type: 'application/javascript' })
const workerScript = URL.createObjectURL(blob)

export default workerScript
