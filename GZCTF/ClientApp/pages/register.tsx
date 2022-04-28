import type { NextPage } from 'next';
import Link from 'next/link';
import {
  Box,
  Stack,
  Group,
  Center,
  Popover,
  PasswordInput,
  Title,
  Progress,
  Text,
  createStyles,
  TextInput,
  Button,
  Anchor,
} from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import MainIcon from './components/icon/MainIcon';
import { useState } from 'react';

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

function PasswordRequirement({ meets, label }: { meets: boolean; label: string }) {
  return (
    <Text color={meets ? 'teal' : 'red'} mt={5} size="sm">
      <Center inline>
        {meets ? <Icon path={mdiCheck} size={0.7} /> : <Icon path={mdiClose} size={0.7} />}
        <Box ml={7}>{label}</Box>
      </Center>
    </Text>
  );
}

const requirements = [
  { re: /[0-9]/, label: 'Includes number' },
  { re: /[a-z]/, label: 'Includes lowercase letter' },
  { re: /[A-Z]/, label: 'Includes uppercase letter' },
  { re: /[$&+,:;=?@#|'<>.^*()%!-]/, label: 'Includes special symbol' },
];

function getStrength(password: string) {
  let multiplier = password.length > 5 ? 0 : 1;

  requirements.forEach((requirement) => {
    if (!requirement.re.test(password)) {
      multiplier += 1;
    }
  });

  return Math.max(100 - (100 / (requirements.length + 1)) * multiplier, 0);
}

const Register: NextPage = () => {
  const [popoverOpened, setPopoverOpened] = useState(false);
  const { classes, cx } = useStyles();
  const [pwd, setPwd] = useInputState('');
  const [uname, setUname] = useInputState('');
  const [email, setEmail] = useInputState('');
  const strength = getStrength(pwd);
  const checks = [
    <PasswordRequirement key={0} label="Has at least 6 chars" meets={pwd.length >= 6} />,
    ...requirements.map((requirement, index) => (
      <PasswordRequirement
        key={index + 1}
        label={requirement.label}
        meets={requirement.re.test(pwd)}
      />
    )),
  ];
  const color = strength === 100 ? 'teal' : strength > 50 ? 'yellow' : 'red';

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
          type="email"
          placeholder="how can I contact you?"
          style={{ width: '100%' }}
          value={email}
          onChange={(event) => setEmail(event.currentTarget.value)}
        />
        <TextInput
          required
          label="Username"
          type="text"
          placeholder="maybe you are called guest?"
          style={{ width: '100%' }}
          value={uname}
          onChange={(event) => setUname(event.currentTarget.value)}
        />
        <Popover
          opened={popoverOpened}
          position="right"
          placement="end"
          withArrow
          styles={{ popover: { width: '100%' }, root: { width: '100%' } }}
          trapFocus={false}
          transition="pop-bottom-left"
          onFocusCapture={() => setPopoverOpened(true)}
          onBlurCapture={() => setPopoverOpened(false)}
          target={
            <PasswordInput
              required
              label="Password"
              placeholder="maybe you need a strong password..."
              value={pwd}
              onChange={(event) => setPwd(event.currentTarget.value)}
            />
          }
        >
          <Progress color={color} value={strength} size={5} style={{ marginBottom: 10 }} />
          {checks}
        </Popover>
        <Link href="/login" passHref={true}>
          <Anchor<'a'>
            sx={(theme) => ({
              color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
              fontWeight: 500,
              fontSize: theme.fontSizes.xs,
              alignSelf: 'end',
            })}
          >
            Already have an account?
          </Anchor>
        </Link>
        <Button fullWidth>Start your journey</Button>
      </Stack>
    </Center>
  );
};

export default Register;
