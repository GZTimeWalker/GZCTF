import type { NextPage } from 'next';
import Link from 'next/link';
import { PasswordInput, Grid, TextInput, Button, Anchor } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import AccountView from '../components/AccountView';
import { AccountService } from '../client';

const Login: NextPage = () => {
  const [pwd, setPwd] = useInputState('');
  const [uname, setUname] = useInputState('');

  return (
    <AccountView>
      <TextInput
        required
        label="用户名或邮箱"
        placeholder="ctfer"
        type="text"
        style={{ width: '100%' }}
        value={uname}
        onChange={(event) => setUname(event.currentTarget.value)}
      />
      <PasswordInput
        required
        label="密码"
        placeholder="P4ssW@rd"
        style={{ width: '100%' }}
        value={pwd}
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
          <Button fullWidth variant="outline">
            登录
          </Button>
        </Grid.Col>
      </Grid>
    </AccountView>
  );
};

export default Login;
