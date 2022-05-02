import type { NextPage } from 'next';
import Link from 'next/link';
import {
  Box,
  Text,
  Button,
  Anchor,
  Center,
  Popover,
  Progress,
  TextInput,
  PasswordInput,
} from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import AccountView from '../../components/AccountView';
import { showNotification } from '@mantine/notifications';
import { useState } from 'react';
import { AccountService } from '../../client';

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
  { re: /[0-9]/, label: '包含数字' },
  { re: /[a-z]/, label: '包含小写字母' },
  { re: /[A-Z]/, label: '包含大写字母' },
  { re: /[$&+,:;=?@#|'<>.^*()%!-]/, label: '包含特殊字符' },
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
  const [pwd, setPwd] = useInputState('');
  const [uname, setUname] = useInputState('');
  const [email, setEmail] = useInputState('');
  const [disabled, setDisabled] = useState(false);

  const strength = getStrength(pwd);
  const checks = [
    <PasswordRequirement key={0} label="至少 6 个字符" meets={pwd.length >= 6} />,
    ...requirements.map((requirement, index) => (
      <PasswordRequirement
        key={index + 1}
        label={requirement.label}
        meets={requirement.re.test(pwd)}
      />
    )),
  ];
  const color = strength === 100 ? 'teal' : strength > 50 ? 'yellow' : 'red';

  const onRegister = () => {
    setDisabled(true);
    AccountService.accountRegister({
      userName: uname,
      password: pwd,
      email,
    })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '一封注册邮件已发送',
          message: '请检查你的邮箱及垃圾邮件~',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
      })
      .catch(() => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: '请稍后重试',
          icon: <Icon path={mdiClose} size={1} />,
        });
        setDisabled(false);
      });
  };

  return (
    <AccountView>
      <TextInput
        required
        label="邮箱"
        type="email"
        placeholder="ctf@example.com"
        style={{ width: '100%' }}
        value={email}
        disabled={disabled}
        onChange={(event) => setEmail(event.currentTarget.value)}
      />
      <TextInput
        required
        label="用户名"
        type="text"
        placeholder="ctfer"
        style={{ width: '100%' }}
        value={uname}
        disabled={disabled}
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
            label="密码"
            placeholder="P4ssW@rd"
            value={pwd}
            disabled={disabled}
            onChange={(event) => setPwd(event.currentTarget.value)}
          />
        }
      >
        <Progress color={color} value={strength} size={5} style={{ marginBottom: 10 }} />
        {checks}
      </Popover>
      <Link href="/account/login" passHref={true}>
        <Anchor<'a'>
          sx={(theme) => ({
            color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
            fontWeight: 500,
            fontSize: theme.fontSizes.xs,
            alignSelf: 'end',
          })}
        >
          已经拥有账户？
        </Anchor>
      </Link>
      <Button fullWidth onClick={onRegister} disabled={disabled}>
        注册
      </Button>
    </AccountView>
  );
};

export default Register;
