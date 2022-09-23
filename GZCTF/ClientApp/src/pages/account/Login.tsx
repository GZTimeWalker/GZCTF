import { FC, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { PasswordInput, Grid, TextInput, Button, Anchor } from '@mantine/core'
import { useInputState, getHotkeyHandler } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'
import { showErrorNotification } from '@Utils/ApiErrorHandler'

const Login: FC = () => {
  const params = useParams()
  const navigate = useNavigate()

  const [pwd, setPwd] = useInputState('')
  const [uname, setUname] = useInputState('')
  const [disabled, setDisabled] = useState(false)

  usePageTitle('登录')

  const onLogin = () => {
    setDisabled(true)

    if (uname.length == 0 || pwd.length < 6) {
      showNotification({
        color: 'red',
        title: '请检查输入',
        message: '无效的用户名或密码',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
      setDisabled(false)
      return
    }

    api.account
      .accountLogIn({
        userName: uname,
        password: pwd,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '登录成功',
          message: '跳转回登录前页面',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        api.account.mutateAccountProfile()
        const from = params['from']
        navigate(from ? (from as string) : '/')
      })
      .catch((err) => {
        showErrorNotification(err)
        setDisabled(false)
      })
  }

  const enterHandler = getHotkeyHandler([['Enter', onLogin]])

  return (
    <AccountView>
      <TextInput
        required
        label="用户名或邮箱"
        placeholder="ctfer"
        type="text"
        style={{ width: '100%' }}
        value={uname}
        disabled={disabled}
        onChange={(event) => setUname(event.currentTarget.value)}
        onKeyDown={enterHandler}
      />
      <PasswordInput
        required
        label="密码"
        id="your-password"
        placeholder="P4ssW@rd"
        style={{ width: '100%' }}
        value={pwd}
        disabled={disabled}
        onChange={(event) => setPwd(event.currentTarget.value)}
        onKeyDown={enterHandler}
      />
      <Anchor
        sx={(theme) => ({
          fontSize: theme.fontSizes.xs,
          alignSelf: 'end',
        })}
        component={Link}
        to="/account/recovery"
      >
        忘记密码？
      </Anchor>
      <Grid grow style={{ width: '100%' }}>
        <Grid.Col span={2}>
          <Button fullWidth variant="outline" component={Link} to="/account/register">
            注册
          </Button>
        </Grid.Col>
        <Grid.Col span={2}>
          <Button fullWidth disabled={disabled} onClick={onLogin}>
            登录
          </Button>
        </Grid.Col>
      </Grid>
    </AccountView>
  )
}

export default Login
