import { ClientError } from '@Utils/Shared'

const INVALID_DATA = 'Data to encrypt cannot be empty'
const INVALID_KEY = 'Invalid public key'
const ALGORITHMS = ['X25519', 'SHA-256', 'AES-GCM']

type Buffer = Uint8Array<ArrayBuffer>

const makeBuffer = (input: number | ArrayBuffer): Buffer => new Uint8Array(input as any)
const b64Encode = (base64: string): Buffer =>
  base64 ? Uint8Array.from(atob(base64), (c) => c.charCodeAt(0)) : makeBuffer(0)
const b64Decode = (bytes: Buffer): string => btoa(String.fromCharCode(...bytes))

async function encryptData(plainTextBytes: Buffer, recipientPublicKeyBase64: string): Promise<Buffer> {
  if (!plainTextBytes || plainTextBytes.length === 0) {
    throw new Error(INVALID_DATA)
  }
  if (!recipientPublicKeyBase64) {
    throw new Error(INVALID_KEY)
  }

  const recipientPublicKeyBytes = b64Encode(recipientPublicKeyBase64)
  if (recipientPublicKeyBytes.length !== 32) {
    throw new Error(INVALID_KEY)
  }

  const subtle = window.crypto.subtle

  const recipientPublicKeyCryptoKey = await subtle.importKey('raw', recipientPublicKeyBytes, ALGORITHMS[0], true, [])

  const ephemeralKeyPair = (await subtle.generateKey(ALGORITHMS[0], true, ['deriveBits'])) as CryptoKeyPair
  const ephemeralPublicKeyBytes = makeBuffer(await subtle.exportKey('raw', ephemeralKeyPair.publicKey!))
  const sharedSecret = await subtle.deriveBits(
    { name: ALGORITHMS[0], public: recipientPublicKeyCryptoKey },
    ephemeralKeyPair.privateKey!,
    256
  )

  const aesKeyBytes = await subtle.digest(ALGORITHMS[1], sharedSecret)
  const aesKeyCryptoKey = await subtle.importKey('raw', aesKeyBytes, { name: ALGORITHMS[2], length: 256 }, false, [
    'encrypt',
  ])

  const nonce = crypto.getRandomValues(makeBuffer(12))
  const ciphertextBytes = makeBuffer(
    await subtle.encrypt({ name: ALGORITHMS[2], iv: nonce, tagLength: 128 }, aesKeyCryptoKey, plainTextBytes)
  )

  const result = makeBuffer(ephemeralPublicKeyBytes.length + nonce.length + ciphertextBytes.length)
  result.set(ephemeralPublicKeyBytes, 0)
  result.set(nonce, ephemeralPublicKeyBytes.length)
  result.set(ciphertextBytes, ephemeralPublicKeyBytes.length + nonce.length)

  return result
}

export const webCryptoAvailable = !!window.crypto?.subtle

export async function encryptApiData(
  t: (key: string) => string,
  plainText: string,
  publicKey?: string | null
): Promise<string> {
  if (!publicKey) {
    return plainText
  }

  if (!webCryptoAvailable) {
    const title = t('common.error.encryption_failed.title')
    const message = t('common.error.encryption_failed.not_secure_contexts')
    console.error(title, message)
    throw new ClientError(title, message)
  }

  try {
    const plainTextBytes = new TextEncoder().encode(plainText)
    const encryptedBytes = await encryptData(plainTextBytes, publicKey)
    return b64Decode(encryptedBytes)
  } catch (error) {
    const title = t('common.error.encryption_failed.title')
    const message = t('common.error.encryption_failed.need_upgrade')
    console.error(title, message, error)
    throw new ClientError(title, message)
  }
}
