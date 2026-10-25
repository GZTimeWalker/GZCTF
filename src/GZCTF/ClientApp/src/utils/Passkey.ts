import { isCancel } from 'axios'
import { TFunction } from 'i18next'
import { showErrorMsg } from '@Utils/Shared'
import api from '@Api'

type CredentialDescriptorJSON = Omit<PublicKeyCredentialDescriptor, 'id'> & { id: string }

export type CreationOptionsJSON = Omit<
  PublicKeyCredentialCreationOptions,
  'challenge' | 'user' | 'excludeCredentials'
> & {
  challenge: string
  user: Omit<PublicKeyCredentialUserEntity, 'id'> & { id: string }
  excludeCredentials?: CredentialDescriptorJSON[]
}

export type RequestOptionsJSON = Omit<PublicKeyCredentialRequestOptions, 'challenge' | 'allowCredentials'> & {
  challenge: string
  allowCredentials?: CredentialDescriptorJSON[]
}

export const supportsPasskeys = () =>
  window.isSecureContext && typeof PublicKeyCredential !== 'undefined' && !!navigator.credentials

const decode = (value: string): ArrayBuffer => {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/')
  return Uint8Array.from(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')), (c) => c.charCodeAt(0)).buffer
}

const encode = (value: ArrayBuffer): string =>
  btoa(Array.from(new Uint8Array(value), (byte) => String.fromCharCode(byte)).join(''))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '')

const serialize = (credential: Credential | null): string => {
  if (!(credential instanceof PublicKeyCredential)) throw new DOMException('No credential', 'NotAllowedError')
  const response = credential.response
  const common = {
    id: credential.id,
    rawId: encode(credential.rawId),
    type: credential.type,
    authenticatorAttachment: credential.authenticatorAttachment,
    clientExtensionResults: credential.getClientExtensionResults(),
  }

  if (response instanceof AuthenticatorAttestationResponse) {
    return JSON.stringify({
      ...common,
      response: {
        clientDataJSON: encode(response.clientDataJSON),
        attestationObject: encode(response.attestationObject),
        transports: response.getTransports?.() ?? [],
      },
    })
  }

  const assertion = response as AuthenticatorAssertionResponse
  return JSON.stringify({
    ...common,
    response: {
      clientDataJSON: encode(assertion.clientDataJSON),
      authenticatorData: encode(assertion.authenticatorData),
      signature: encode(assertion.signature),
      userHandle: assertion.userHandle ? encode(assertion.userHandle) : null,
    },
  })
}

export const passkeyHeaders = async (signal?: AbortSignal) => {
  const { data } = await api.passkey.passkeyToken({ signal })
  return { 'X-CSRF-TOKEN': data.token }
}

export const loginWithPasskey = async (signal: AbortSignal) => {
  const headers = await passkeyHeaders(signal)
  const { data }: { data: RequestOptionsJSON } = await api.passkey.passkeyRequestOptions({ headers, signal })
  const credential = await navigator.credentials.get({
    publicKey: {
      ...data,
      challenge: decode(data.challenge),
      allowCredentials: data.allowCredentials?.map((c) => ({ ...c, id: decode(c.id) })),
    },
    signal,
  })
  await api.passkey.passkeyLogIn({ credentialJson: serialize(credential) }, { headers, signal })
}

export const registerPasskey = async (name: string, password: string, signal: AbortSignal) => {
  const headers = await passkeyHeaders(signal)
  const { data }: { data: CreationOptionsJSON } = await api.passkey.passkeyCreationOptions(
    { password },
    { headers, signal }
  )
  const credential = await navigator.credentials.create({
    publicKey: {
      ...data,
      challenge: decode(data.challenge),
      user: { ...data.user, id: decode(data.user.id) },
      excludeCredentials: data.excludeCredentials?.map((c) => ({ ...c, id: decode(c.id) })),
    },
    signal,
  })
  await api.passkey.passkeyRegister({ name, credentialJson: serialize(credential) }, { headers, signal })
}

export const showPasskeyError = (error: unknown, t: TFunction) => {
  if (isCancel(error)) return
  if (error instanceof DOMException) {
    let key = 'failed'
    switch (error.name) {
      case 'AbortError':
        return
      case 'NotAllowedError':
        key = 'cancelled'
        break
      case 'InvalidStateError':
        key = 'already_registered'
        break
      case 'NotSupportedError':
        key = 'unsupported'
        break
      default:
        console.warn(error)
    }
    showErrorMsg(t(`account.passkey.${key}`), t)
    return
  }
  showErrorMsg(error, t)
}
