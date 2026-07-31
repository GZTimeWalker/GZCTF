import {
  Badge,
  Button,
  Card,
  Group,
  Modal,
  Stack,
  Text,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDelete, mdiKey } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { showErrorMsg } from '@Utils/Shared'
import { base64urlToBuffer, bufferToBase64url } from '@Utils/WebAuthn'
import api, { PasskeyInfoModel } from '@Api'
import dayjs from 'dayjs'

interface PasskeyManagerProps {
  opened: boolean
  onClose: () => void
}

export const PasskeyManager: FC<PasskeyManagerProps> = ({ opened, onClose }) => {
  const { t } = useTranslation()
  const [passkeys, setPasskeys] = useState<PasskeyInfoModel[]>([])
  const [passkeyName, setPasskeyName] = useState('')
  const [addingPasskey, setAddingPasskey] = useState(false)

  const fetchPasskeys = async () => {
    try {
      const res = await api.account.accountPasskeys()
      setPasskeys(res.data)
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  useEffect(() => {
    if (opened) {
      fetchPasskeys()
    }
  }, [opened])

  const isPasskeySupported = () => {
    return (
      window.PublicKeyCredential !== undefined &&
      typeof window.PublicKeyCredential === 'function'
    )
  }

  const addPasskey = async () => {
    if (!isPasskeySupported()) {
      showNotification({
        color: 'red',
        title: t('account.passkey.not_supported'),
        message: '',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setAddingPasskey(true)
    try {
      // Step 1: Get attestation options from server
      const optionsRes = await api.account.accountPasskeyAttestationOptions()
      const options = optionsRes.data as PublicKeyCredentialCreationOptions

      // Convert base64url strings to ArrayBuffers for WebAuthn API
      const publicKeyOptions: PublicKeyCredentialCreationOptions = {
        ...options,
        challenge: base64urlToBuffer(options.challenge as unknown as string),
        user: {
          ...options.user,
          id: base64urlToBuffer(options.user.id as unknown as string),
        },
        excludeCredentials: options.excludeCredentials?.map((cred) => ({
          ...cred,
          id: base64urlToBuffer(cred.id as unknown as string),
        })),
      }

      // Step 2: Create credential using WebAuthn API
      const credential = (await navigator.credentials.create({
        publicKey: publicKeyOptions,
      })) as PublicKeyCredential

      if (!credential) {
        throw new Error('Failed to create credential')
      }

      // Step 3: Send credential to server
      const response = credential.response as AuthenticatorAttestationResponse
      const credentialJson = JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,
        response: {
          clientDataJSON: bufferToBase64url(response.clientDataJSON),
          attestationObject: bufferToBase64url(response.attestationObject),
          transports: response.getTransports?.() ?? [],
        },
        clientExtensionResults: credential.getClientExtensionResults(),
      })

      await api.account.accountPasskeyAttestation({
        credentialJson,
        name: passkeyName || undefined,
      })

      showNotification({
        color: 'teal',
        title: t('account.passkey.add_success'),
        message: '',
        icon: <Icon path={mdiCheck} size={1} />,
      })

      setPasskeyName('')
      fetchPasskeys()
    } catch (e: any) {
      if (e.name !== 'NotAllowedError') {
        showErrorMsg(e, t)
      }
    } finally {
      setAddingPasskey(false)
    }
  }

  const deletePasskey = async (credentialId: string) => {
    try {
      await api.account.accountDeletePasskey(credentialId)
      showNotification({
        color: 'teal',
        title: t('account.passkey.delete_success'),
        message: '',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      fetchPasskeys()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title={t('account.passkey.title')} size="lg">
      <Stack>
        <Text size="sm" c="dimmed">
          {t('account.passkey.description')}
        </Text>

        {passkeys.length === 0 ? (
          <Text c="dimmed" ta="center" py="md">
            {t('account.passkey.no_passkeys')}
          </Text>
        ) : (
          <Stack gap="sm">
            {passkeys.map((passkey) => (
              <Card key={passkey.credentialId} withBorder padding="sm">
                <Group justify="space-between" wrap="nowrap">
                  <Group gap="sm" wrap="nowrap">
                    <Icon path={mdiKey} size={1} />
                    <Stack gap={4}>
                      <Text fw={500}>{passkey.name || passkey.credentialId.slice(0, 16) + '...'}</Text>
                      <Group gap="xs">
                        <Text size="xs" c="dimmed">
                          {t('account.passkey.created_at')}: {dayjs(passkey.createdAt).format('YYYY-MM-DD HH:mm')}
                        </Text>
                        {passkey.isBackedUp && (
                          <Badge size="xs" variant="light">
                            {t('account.passkey.backed_up')}
                          </Badge>
                        )}
                      </Group>
                    </Stack>
                  </Group>
                  <ActionIconWithConfirm
                    iconPath={mdiDelete}
                    color="red"
                    message={t('account.passkey.confirm_delete')}
                    onClick={() => deletePasskey(passkey.credentialId)}
                  />
                </Group>
              </Card>
            ))}
          </Stack>
        )}

        <Group gap="sm" mt="md">
          <TextInput
            placeholder={t('account.passkey.name_placeholder')}
            value={passkeyName}
            onChange={(e) => setPasskeyName(e.target.value)}
            style={{ flex: 1 }}
          />
          <Button
            leftSection={<Icon path={mdiKey} size={0.9} />}
            loading={addingPasskey}
            onClick={addPasskey}
            disabled={!isPasskeySupported()}
          >
            {t('account.button.add_passkey')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default PasskeyManager
