import type { NextPage } from 'next';
import Link from 'next/link';
import {
  Stack,
  Group,
  Center,
  Title,
  createStyles,
  TextInput,
  Button,
  Anchor,
} from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import MainIcon from './components/icon/MainIcon';

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

const Recovery: NextPage = () => {
  const { classes, cx } = useStyles();
  const [email, setEmail] = useInputState('');

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
          label="Email"
          placeholder="just give me your email~"
          type="text"
          style={{ width: '100%' }}
          value={email}
          onChange={(event) => setEmail(event.currentTarget.value)}
        />
        <Link href="/login" passHref={true}>
          <Anchor<'a'>
            sx={(theme) => ({
              color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
              fontWeight: 500,
              fontSize: theme.fontSizes.xs,
              alignSelf: 'end',
            })}
          >
            Ready to login?
          </Anchor>
        </Link>
        <Button fullWidth>Send reset email</Button>
      </Stack>
    </Center>
  );
};

export default Recovery;
