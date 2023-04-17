import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, PasswordInput, Stack } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api from '@Api'
import StrengthPasswordInput from './StrengthPasswordInput'

const PasswordChangeModal: FC<ModalProps> = (props) => {
  const [oldPwd, setOldPwd] = useInputState('')
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')

  const navigate = useNavigate()

  const onChangePwd = () => {
    if (!pwd || !retypedPwd) {
      showNotification({
        color: 'red',
        title: '密码不能为空',
        message: '请检查你的输入',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
    } else if (pwd === retypedPwd) {
      api.account
        .accountChangePassword({
          old: oldPwd,
          new: pwd,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            message: '密码已修改，请重新登录',
            icon: <Icon path={mdiCheck} size={1} />,
            withCloseButton: false,
          })
          props.onClose()
          api.account.accountLogOut()
          navigate('/account/login')
        })
        .catch(showErrorNotification)
    } else {
      showNotification({
        color: 'red',
        title: '密码不一致',
        message: '请检查你的输入',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <PasswordInput
          required
          label="原密码"
          placeholder="P4ssW@rd"
          style={{ width: '100%' }}
          value={oldPwd}
          onChange={setOldPwd}
        />
        <StrengthPasswordInput value={pwd} onChange={setPwd} />
        <PasswordInput
          required
          label="确认密码"
          placeholder="P4ssW@rd"
          style={{ width: '100%' }}
          value={retypedPwd}
          onChange={setRetypedPwd}
        />

        <Group position="right">
          <Button
            variant="default"
            onClick={() => {
              setOldPwd('')
              setPwd('')
              setRetypedPwd('')
              props.onClose()
            }}
          >
            取消
          </Button>
          <Button color="orange" onClick={onChangePwd}>
            确认修改
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default PasswordChangeModal
