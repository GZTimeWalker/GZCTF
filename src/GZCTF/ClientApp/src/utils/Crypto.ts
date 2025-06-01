import { showErrorNotification } from '@Utils/Shared'

const INVALID_DATA = 'Data to encrypt cannot be empty'
const INVALID_KEY = 'Invalid public key'
const ALGORITHMS = ['X25519', 'SHA-256', 'AES-GCM']

const makeArray = (length: number): Uint8Array => new Uint8Array(length)
const toArray = (array: ArrayBuffer | ArrayLike<number>): Uint8Array => new Uint8Array(array)

function base64ToUint8Array(base64: string): Uint8Array {
  const binaryString = atob(base64)
  const len = binaryString.length
  const bytes = makeArray(len)
  for (let i = 0; i < len; i++) {
    bytes[i] = binaryString.charCodeAt(i)
  }
  return bytes
}

function uint8ArrayToBase64(bytes: Uint8Array): string {
  let binary = ''
  const len = bytes.byteLength
  for (let i = 0; i < len; i++) {
    binary += String.fromCharCode(bytes[i])
  }
  return btoa(binary)
}

async function encryptData(plainTextBytes: Uint8Array, recipientPublicKeyBase64: string): Promise<Uint8Array> {
  if (!plainTextBytes || plainTextBytes.length === 0) {
    throw new Error(INVALID_DATA)
  }
  if (!recipientPublicKeyBase64) {
    throw new Error(INVALID_KEY)
  }

  const recipientPublicKeyBytes = base64ToUint8Array(recipientPublicKeyBase64)
  if (recipientPublicKeyBytes.length !== 32) {
    throw new Error(INVALID_KEY)
  }

  const subtle = window.crypto.subtle

  const recipientPublicKeyCryptoKey = await subtle.importKey('raw', recipientPublicKeyBytes, ALGORITHMS[0], true, [])

  const ephemeralKeyPair = (await subtle.generateKey(ALGORITHMS[0], true, ['deriveBits'])) as CryptoKeyPair
  const ephemeralPublicKeyBytes = toArray(await subtle.exportKey('raw', ephemeralKeyPair.publicKey!))
  const sharedSecret = toArray(
    await subtle.deriveBits(
      { name: ALGORITHMS[0], public: recipientPublicKeyCryptoKey },
      ephemeralKeyPair.privateKey!,
      256
    )
  )

  const aesKeyBytes = toArray(await subtle.digest(ALGORITHMS[1], sharedSecret))
  const aesKeyCryptoKey = await subtle.importKey('raw', aesKeyBytes, { name: ALGORITHMS[2], length: 256 }, false, [
    'encrypt',
  ])

  const nonce = crypto.getRandomValues(makeArray(12))
  const ciphertextBytes = toArray(
    await subtle.encrypt({ name: ALGORITHMS[2], iv: nonce, tagLength: 128 }, aesKeyCryptoKey, plainTextBytes)
  )

  const result = makeArray(ephemeralPublicKeyBytes.length + nonce.length + ciphertextBytes.length)
  result.set(ephemeralPublicKeyBytes, 0)
  result.set(nonce, ephemeralPublicKeyBytes.length)
  result.set(ciphertextBytes, ephemeralPublicKeyBytes.length + nonce.length)

  return result
}

export const isWebCryptoAvailable = (): boolean => !!window.crypto?.subtle

export async function encryptApiData(
  t: (key: string) => string,
  plainText: string,
  publicKey?: string | null
): Promise<string> {
  if (!publicKey) {
    return plainText
  }

  if (!isWebCryptoAvailable()) {
    const message = t('common.error.encryption_failed.not_secure_contexts')
    showErrorNotification(t('common.error.encryption_failed.title'), message)
    throw new Error(message)
  }

  try {
    const plainTextBytes = new TextEncoder().encode(plainText)
    const encryptedBytes = await encryptData(plainTextBytes, publicKey)
    return uint8ArrayToBase64(encryptedBytes)
  } catch (error) {
    const title = t('common.error.encryption_failed.title')
    const message = t('common.error.encryption_failed.need_upgrade')
    console.error(message, error)
    showErrorNotification(title, message)
    throw error
  }
}
