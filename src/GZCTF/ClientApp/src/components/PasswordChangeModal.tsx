import { Button, Group, Modal, ModalProps, PasswordInput, Stack } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { StrengthPasswordInput } from '@Components/StrengthPasswordInput'
import { encryptApiData } from '@Utils/Crypto'
import { showErrorMsg } from '@Utils/Shared'
import { useConfig } from '@Hooks/useConfig'
import api from '@Api'

export const PasswordChangeModal: FC<ModalProps> = (props) => {
  const [oldPwd, setOldPwd] = useInputState('')
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')

  const navigate = useNavigate()

  const { t } = useTranslation()
  const { config } = useConfig()

  const onChangePwd = async () => {
    if (!pwd || !retypedPwd) {
      showNotification({
        color: 'red',
        title: t('account.password.empty'),
        message: t('common.error.check_input'),
        icon: <Icon path={mdiClose} size={1} />,
      })
    } else if (pwd === retypedPwd) {
      try {
        await api.account.accountChangePassword({
          old: await encryptApiData(t, oldPwd, config.apiPublicKey),
          new: await encryptApiData(t, pwd, config.apiPublicKey),
        })
        showNotification({
          color: 'teal',
          message: t('account.notification.profile.password_updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        props.onClose()
        api.account.accountLogOut()
        navigate('/account/login')
      } catch (e) {
        showErrorMsg(e, t)
      }
    } else {
      showNotification({
        color: 'red',
        title: t('account.password.not_match'),
        message: t('common.error.check_input'),
        icon: <Icon path={mdiClose} size={1} />,
      })
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <PasswordInput
          required
          label={t('account.label.password_old')}
          placeholder="P4ssW@rd"
          w="100%"
          value={oldPwd}
          onChange={setOldPwd}
        />
        <StrengthPasswordInput value={pwd} onChange={setPwd} />
        <PasswordInput
          required
          label={t('account.label.password_retype')}
          placeholder="P4ssW@rd"
          w="100%"
          value={retypedPwd}
          onChange={setRetypedPwd}
        />

        <Group justify="right">
          <Button
            variant="default"
            onClick={() => {
              setOldPwd('')
              setPwd('')
              setRetypedPwd('')
              props.onClose()
            }}
          >
            {t('common.modal.cancel')}
          </Button>
          <Button color="orange" onClick={onChangePwd}>
            {t('common.modal.confirm_update')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
