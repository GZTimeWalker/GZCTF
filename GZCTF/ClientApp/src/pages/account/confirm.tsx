import type { NextPage } from 'next';
import AccountView from '../../components/AccountView';
import { showNotification } from '@mantine/notifications';
import { useRouter } from 'next/router';
import { mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import { AccountService } from '../../client';
import { useEffect } from 'react';
import { Text } from '@mantine/core';

const Confirm: NextPage = () => {
  const router = useRouter();
  const token = router.query['token'];
  const email = router.query['email'];

  useEffect(() => {
    if (token && email && typeof token === 'string' && typeof email === 'string') {
      AccountService.accountMailChangeConfirm({ token, email })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '邮箱已验证',
            message: Buffer.from(email, 'base64'),
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          router.push('/');
        })
        .catch(() => {
          showNotification({
            color: 'red',
            title: '邮箱验证失败',
            message: '参数错误，请检查',
            icon: <Icon path={mdiClose} size={1} />,
            disallowClose: true,
          });
        });
    } else {
      showNotification({
        color: 'red',
        title: '邮箱验证失败',
        message: '参数错误，请检查',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      });
    }
  });

  return (
    <AccountView>
      <Text>验证邮箱中……</Text>
    </AccountView>
  );
};

export default Confirm;
