import { FC } from 'react'
import { Link } from 'react-router-dom'
import { TextInput, Button, Anchor } from '@mantine/core'
import { getHotkeyHandler, useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../Api'
import AccountView from '../../components/AccountView'
import { showErrorNotification } from '../../utils/ApiErrorHandler'

const Recovery: FC = () => {
  const [email, setEmail] = useInputState('')

  const onRecovery = () => {
    api.account
      .accountRecovery({
        email,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '一封恢复邮件已发送',
          message: '请检查你的邮箱及垃圾邮件~',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
      })
      .catch(showErrorNotification)
  }

  return (
    <AccountView>
      <TextInput
        required
        label="邮箱"
        placeholder="ctf@example.com"
        type="email"
        style={{ width: '100%' }}
        value={email}
        onChange={(event) => setEmail(event.currentTarget.value)}
        onKeyDown={getHotkeyHandler([['Enter', onRecovery]])}
      />
      <Anchor
        sx={(theme) => ({
          fontSize: theme.fontSizes.xs,
          alignSelf: 'end',
        })}
        component={Link}
        to="/account/login"
      >
        准备好登录？
      </Anchor>
      <Button fullWidth onClick={onRecovery}>
        发送重置邮件
      </Button>
    </AccountView>
  )
}

export default Recovery
