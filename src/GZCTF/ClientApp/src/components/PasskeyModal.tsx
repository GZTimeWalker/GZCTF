import { Alert, Button, Group, Loader, Modal, PasswordInput, Stack, Text, TextInput } from '@mantine/core'
import { modals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import dayjs from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { encryptApiData } from '@Utils/Crypto'
import { passkeyHeaders, registerPasskey, showPasskeyError, supportsPasskeys } from '@Utils/Passkey'
import { useConfig } from '@Hooks/useConfig'
import api, { PasskeyModel } from '@Api'

export const PasskeyModal: FC<{ opened: boolean; onClose: () => void }> = ({ opened, onClose }) => {
  const { t } = useTranslation()
  const { config } = useConfig()
  const {
    data: passkeys,
    error,
    isLoading,
    mutate,
  } = api.passkey.usePasskeyList({ revalidateOnFocus: false }, opened)
  const [editing, setEditing] = useState<PasskeyModel | 'new' | null>(null)
  const [name, setName] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const operation = useRef<AbortController | null>(null)

  useEffect(() => () => operation.current?.abort(), [])

  const closeEditor = () => {
    operation.current?.abort()
    setEditing(null)
    setPassword('')
    setName('')
  }

  const save = async () => {
    if (busy || !editing || !name.trim()) return
    setBusy(true)
    const controller = new AbortController()
    operation.current = controller
    try {
      if (editing === 'new') {
        await registerPasskey(name.trim(), await encryptApiData(t, password, config.apiPublicKey), controller.signal)
      } else {
        await api.passkey.passkeyRename(
          editing.credentialId,
          { name: name.trim() },
          { headers: await passkeyHeaders() }
        )
      }
      showNotification({ color: 'teal', message: t('account.passkey.saved') })
      closeEditor()
      await mutate()
    } catch (err) {
      showPasskeyError(err, t)
    } finally {
      setBusy(false)
    }
  }

  const remove = (passkey: PasskeyModel) => {
    modals.openConfirmModal({
      title: t('account.passkey.remove'),
      children: (
        <Text>{t('account.passkey.remove_confirm', { name: passkey.name || t('account.passkey.unnamed') })}</Text>
      ),
      labels: { confirm: t('account.passkey.remove'), cancel: t('common.modal.cancel') },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          await api.passkey.passkeyRemove(passkey.credentialId, { headers: await passkeyHeaders() })
          showNotification({ color: 'teal', message: t('account.passkey.removed') })
          await mutate()
        } catch (err) {
          showPasskeyError(err, t)
        } finally {
          setBusy(false)
        }
      },
    })
  }

  return (
    <>
      <Modal opened={opened} onClose={onClose} title={t('account.passkey.title')} size="lg">
        <Stack>
          <Text size="sm" c="dimmed">
            {t('account.passkey.description')}
          </Text>
          {!supportsPasskeys() && <Alert color="yellow">{t('account.passkey.unsupported')}</Alert>}
          {isLoading && <Loader mx="auto" />}
          {error && <Alert color="red">{t('account.passkey.load_failed')}</Alert>}
          {passkeys?.map((passkey) => (
            <Group key={passkey.credentialId} justify="space-between" wrap="wrap">
              <Stack gap={0} style={{ overflowWrap: 'anywhere', flex: 1 }}>
                <Text>{passkey.name || t('account.passkey.unnamed')}</Text>
                <Text size="xs" c="dimmed">
                  {t('account.passkey.created', { date: dayjs(passkey.createdAt).format('LLL') })}
                </Text>
              </Stack>
              <Group gap="xs">
                <Button
                  size="xs"
                  variant="subtle"
                  disabled={busy}
                  onClick={() => {
                    setName(passkey.name ?? '')
                    setEditing(passkey)
                  }}
                >
                  {t('account.passkey.rename')}
                </Button>
                <Button size="xs" variant="subtle" color="red" disabled={busy} onClick={() => remove(passkey)}>
                  {t('account.passkey.remove')}
                </Button>
              </Group>
            </Group>
          ))}
          <Button
            disabled={busy || !supportsPasskeys() || !passkeys || passkeys.length >= 10}
            onClick={() => {
              setName('')
              setPassword('')
              setEditing('new')
            }}
          >
            {t('account.passkey.add')}
          </Button>
          {passkeys && passkeys.length >= 10 && (
            <Text size="sm" c="dimmed">
              {t('account.passkey.limit')}
            </Text>
          )}
        </Stack>
      </Modal>
      <Modal
        opened={editing !== null}
        onClose={closeEditor}
        title={t(editing === 'new' ? 'account.passkey.add' : 'account.passkey.rename')}
      >
        <form
          onSubmit={(event) => {
            event.preventDefault()
            void save()
          }}
        >
          <Stack>
            <TextInput
              required
              maxLength={100}
              label={t('account.passkey.name')}
              value={name}
              disabled={busy}
              onChange={(event) => setName(event.currentTarget.value)}
            />
            {editing === 'new' && (
              <PasswordInput
                required
                autoComplete="current-password"
                label={t('account.label.password')}
                description={t('account.passkey.confirm_password')}
                value={password}
                disabled={busy}
                onChange={(event) => setPassword(event.currentTarget.value)}
              />
            )}
            <Group justify="end">
              <Button variant="default" onClick={closeEditor}>
                {t('common.modal.cancel')}
              </Button>
              <Button type="submit" loading={busy} disabled={!name.trim() || (editing === 'new' && !password)}>
                {t('common.modal.confirm')}
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>
    </>
  )
}
