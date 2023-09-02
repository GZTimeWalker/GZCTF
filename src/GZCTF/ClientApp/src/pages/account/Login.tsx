import { FC, useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { PasswordInput, Grid, TextInput, Button, Anchor, Box } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useCaptcha } from '@Utils/useCaptcha'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUser } from '@Utils/useUser'
import api from '@Api'

const Login: FC = () => {
  const params = useSearchParams()[0]
  const captcha = useCaptcha('login')
  const navigate = useNavigate()

  const [pwd, setPwd] = useInputState('')
  const [uname, setUname] = useInputState('')
  const [disabled, setDisabled] = useState(false)
  const [needRedirect, setNeedRedirect] = useState(false)

  const { user, mutate } = useUser()

  usePageTitle('登录')

  useEffect(() => {
    if (needRedirect && user) {
      setNeedRedirect(false)
      setTimeout(() => {
        navigate(params.get('from') ?? '/')
      }, 200)
    }
  }, [user, needRedirect])

  const onLogin = async (event: React.FormEvent) => {
    event.preventDefault()

    setDisabled(true)

    if (uname.length === 0 || pwd.length < 6) {
      showNotification({
        color: 'red',
        title: '请检查输入',
        message: '无效的用户名或密码',
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    const token = await captcha?.getChallenge()

    api.account
      .accountLogIn({
        userName: uname,
        password: pwd,
        challenge: token,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '登录成功',
          message: '跳转回登录前页面',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setNeedRedirect(true)
        mutate()
      })
      .catch((err) => {
        showErrorNotification(err)
        setDisabled(false)
      })
  }

  return (
    <AccountView onSubmit={onLogin}>
      <TextInput
        required
        label="用户名或邮箱"
        placeholder="ctfer"
        type="text"
        w="100%"
        value={uname}
        disabled={disabled}
        onChange={(event) => setUname(event.currentTarget.value)}
      />
      <PasswordInput
        required
        label="密码"
        id="your-password"
        placeholder="P4ssW@rd"
        w="100%"
        value={pwd}
        disabled={disabled}
        onChange={(event) => setPwd(event.currentTarget.value)}
      />
      <Box id="captcha" />
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
      <Grid grow w="100%">
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
