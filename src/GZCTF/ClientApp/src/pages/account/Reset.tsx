import { FC, useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Button, PasswordInput } from '@mantine/core'
import { getHotkeyHandler, useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import StrengthPasswordInput from '@Components/StrengthPasswordInput'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Reset: FC = () => {
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const navigate = useNavigate()
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')
  const [disabled, setDisabled] = useState(false)

  usePageTitle('重置密码')

  const onReset = () => {
    if (pwd !== retypedPwd) {
      showNotification({
        color: 'red',
        title: '请检查输入',
        message: '重复密码有误',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
      return
    }

    if (!(token && email)) {
      showNotification({
        color: 'red',
        title: '密码重设失败',
        message: '参数错误，请检查',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
      return
    }

    setDisabled(true)
    api.account
      .accountPasswordReset({
        rToken: token,
        email: email,
        password: pwd,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '密码已重置',
          message: '请重新登录',
          icon: <Icon path={mdiCheck} size={1} />,
          withCloseButton: false,
        })
        navigate('/account/login')
      })
      .catch((err) => {
        showErrorNotification(err)
        setDisabled(false)
      })
  }

  const enterHandler = getHotkeyHandler([['Enter', onReset]])

  return (
    <AccountView>
      <StrengthPasswordInput
        value={pwd}
        onChange={(event) => setPwd(event.currentTarget.value)}
        label="新密码"
        disabled={disabled}
        onKeyDown={enterHandler}
      />
      <PasswordInput
        required
        value={retypedPwd}
        onChange={(event) => setRetypedPwd(event.currentTarget.value)}
        label="重复密码"
        w="100%"
        disabled={disabled}
        error={pwd !== retypedPwd}
        onKeyDown={enterHandler}
      />
      <Button fullWidth onClick={onReset} disabled={disabled}>
        重置密码
      </Button>
    </AccountView>
  )
}

export default Reset
