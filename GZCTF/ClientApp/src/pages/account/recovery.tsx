import type { NextPage } from 'next';
import Link from 'next/link';
import { TextInput, Button, Anchor } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import AccountView from '../../components/AccountView';

const Recovery: NextPage = () => {
  const [email, setEmail] = useInputState('');

  return (
    <AccountView>
      <TextInput
        required
        label="邮箱"
        placeholder="ctf@example.com"
        type="text"
        style={{ width: '100%' }}
        value={email}
        onChange={(event) => setEmail(event.currentTarget.value)}
      />
      <Link href="/account/login" passHref={true}>
        <Anchor<'a'>
          sx={(theme) => ({
            color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
            fontWeight: 500,
            fontSize: theme.fontSizes.xs,
            alignSelf: 'end',
          })}
        >
          准备好登录？
        </Anchor>
      </Link>
      <Button fullWidth>发送重置邮件</Button>
    </AccountView>
  );
};

export default Recovery;
