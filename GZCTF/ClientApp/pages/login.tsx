import type { NextPage } from 'next';
import Link from 'next/link';
import {
  Stack,
  Group,
  Center,
  PasswordInput,
  Title,
  Grid,
  createStyles,
  TextInput,
  Button,
  Anchor,
} from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { MainIcon } from './components/icon/MainIcon';

const useStyles = createStyles((theme) => ({
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  title: {
    marginLeft: '-20px',
    marginBottom: '-5px',
  },
  input: {
    minWidth: '20%',
  },
}));

const Login: NextPage = () => {
  const { classes, cx } = useStyles();
  const [pwd, setPwd] = useInputState('');
  const [uname, setUname] = useInputState('');

  return (
    <Center style={{ height: '100vh' }}>
      <Stack align="center" justify="center" className={cx(classes.input)}>
        <Group>
          <MainIcon style={{ maxWidth: 60, height: 'auto' }} />
          <Title className={cx(classes.title)}>
            GZ<span className={cx(classes.brand)}>::</span>CTF
          </Title>
        </Group>
        <TextInput
          required
          label="Username or Email"
          placeholder="welcome back!"
          type="text"
          style={{ width: '100%' }}
          value={uname}
          onChange={(event) => setUname(event.currentTarget.value)}
        />
        <PasswordInput
          required
          label="Password"
          placeholder="I won't peek..."
          style={{ width: '100%' }}
          value={pwd}
          onChange={(event) => setPwd(event.currentTarget.value)}
        />
        <Link href="/recovery" passHref={true}>
          <Anchor<'a'>
            sx={(theme) => ({
              color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
              fontWeight: 500,
              fontSize: theme.fontSizes.xs,
              alignSelf: 'end',
            })}
          >
            Forgot your password?
          </Anchor>
        </Link>
        <Grid grow style={{ width: '100%' }}>
          <Grid.Col span={2}>
            <Button fullWidth variant="outline" >Login</Button>
          </Grid.Col>
          <Grid.Col span={2}>
            <Link href="/register" passHref={true}>
              <Button fullWidth>Register</Button>
            </Link>
          </Grid.Col>
        </Grid>
      </Stack>
    </Center>
  );
};

export default Login;
