function base64ToUint8Array(base64: string): Uint8Array {
  const binaryString = atob(base64)
  const len = binaryString.length
  const bytes = new Uint8Array(len)
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
    throw new Error('Data to encrypt cannot be empty.')
  }
  if (!recipientPublicKeyBase64) {
    throw new Error('Recipient public key cannot be empty.')
  }

  const recipientPublicKeyBytes = base64ToUint8Array(recipientPublicKeyBase64)
  if (recipientPublicKeyBytes.length !== 32) {
    throw new Error('Invalid X25519 public key length.')
  }

  const recipientPublicKeyCryptoKey = await crypto.subtle.importKey('raw', recipientPublicKeyBytes, 'X25519', true, [])

  const ephemeralKeyPair = (await crypto.subtle.generateKey('X25519', true, ['deriveBits'])) as CryptoKeyPair

  const ephemeralPublicKeyBytes = new Uint8Array(await crypto.subtle.exportKey('raw', ephemeralKeyPair.publicKey!))

  const sharedSecret = new Uint8Array(
    await crypto.subtle.deriveBits(
      { name: 'X25519', public: recipientPublicKeyCryptoKey },
      ephemeralKeyPair.privateKey!,
      256
    )
  )

  const aesKeyBytes = new Uint8Array(await crypto.subtle.digest('SHA-256', sharedSecret))

  const aesKeyCryptoKey = await crypto.subtle.importKey('raw', aesKeyBytes, { name: 'AES-GCM', length: 256 }, false, [
    'encrypt',
  ])

  const nonce = crypto.getRandomValues(new Uint8Array(12))

  const ciphertextArrayBuffer = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv: nonce, tagLength: 128 },
    aesKeyCryptoKey,
    plainTextBytes
  )
  const ciphertextBytes = new Uint8Array(ciphertextArrayBuffer)

  const result = new Uint8Array(ephemeralPublicKeyBytes.length + nonce.length + ciphertextBytes.length)
  result.set(ephemeralPublicKeyBytes, 0)
  result.set(nonce, ephemeralPublicKeyBytes.length)
  result.set(ciphertextBytes, ephemeralPublicKeyBytes.length + nonce.length)

  return result
}

function isWebCryptoAvailable(): boolean {
  return typeof crypto !== 'undefined' && typeof crypto.subtle !== 'undefined'
}

export async function encryptApiData(plainText: string, publicKey?: string | null): Promise<string> {
  if (!publicKey) {
    return plainText
  }

  if (!isWebCryptoAvailable()) {
    throw new Error('Web Crypto API is not available in this environment.')
  }

  const plainTextBytes = new TextEncoder().encode(plainText)
  const encryptedBytes = await encryptData(plainTextBytes, publicKey)
  return uint8ArrayToBase64(encryptedBytes)
}
