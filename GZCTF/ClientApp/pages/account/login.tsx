import type { NextPage } from 'next';
import Link from 'next/link';
import { useRouter } from 'next/router'
import { useState } from 'react';
import { PasswordInput, Grid, TextInput, Button, Anchor } from '@mantine/core';
import { useInputState, useWindowEvent } from '@mantine/hooks';
import AccountView from '../components/AccountView';
import { AccountService } from '../client';
import { mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import { showNotification } from '@mantine/notifications';

const Login: NextPage = () => {
  const router = useRouter()
  const [pwd, setPwd] = useInputState('');
  const [uname, setUname] = useInputState('');
  const [disabled, setDisabled] = useState(false);

  const onLogin = () => {
    setDisabled(true);

    if(uname.length == 0 || pwd.length < 6){
      showNotification({
        color: 'red',
        title: '请检查输入',
        message: '无效的用户名或密码',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      });
      setDisabled(false);
      return;
    }

    AccountService.accountLogIn({
      userName: uname,
      password: pwd,
    })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '登陆成功',
          message: '跳转回登录前页面',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
        let from = router.query['from'];
        router.push(from ? from as string : '/')
      })
      .catch((_) => {
        showNotification({
          color: 'red',
          title: '登陆失败',
          message: '无效的用户名或密码',
          icon: <Icon path={mdiClose} size={1} />,
          disallowClose: true,
        });
        setDisabled(false);
      });
  };

  useWindowEvent('keydown', (e) => {
    console.log(e.code)
    if(e.code == 'Enter' || e.code == 'NumpadEnter') {
      onLogin()
    }
  })

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
      />
      <PasswordInput
        required
        label="密码"
        placeholder="P4ssW@rd"
        style={{ width: '100%' }}
        value={pwd}
        disabled={disabled}
        onChange={(event) => setPwd(event.currentTarget.value)}
      />
      <Link href="/account/recovery" passHref={true}>
        <Anchor<'a'>
          sx={(theme) => ({
            color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
            fontWeight: 500,
            fontSize: theme.fontSizes.xs,
            alignSelf: 'end',
          })}
        >
          忘记密码？
        </Anchor>
      </Link>
      <Grid grow style={{ width: '100%' }}>
        <Grid.Col span={2}>
          <Link href="/account/register" passHref={true}>
            <Button fullWidth>注册</Button>
          </Link>
        </Grid.Col>
        <Grid.Col span={2}>
          <Button fullWidth variant="outline" disabled={disabled} onClick={onLogin}>
            登录
          </Button>
        </Grid.Col>
      </Grid>
    </AccountView>
  );
};

export default Login;
