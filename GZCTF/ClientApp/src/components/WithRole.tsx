import React, { FC } from 'react';
import { useEffect } from 'react';
import { useRouter } from 'next/router';
import { Center, Loader } from '@mantine/core';
import api, { Role } from '../Api';

interface WithRoleProps {
  requiredRole: Role;
  children: JSX.Element;
}

const RoleMap = new Map<Role, number>([
  [Role.Admin, 3],
  [Role.Monitor, 1],
  [Role.User, 0],
  [Role.BannedUser, -1],
]);

const WithRole: FC<WithRoleProps> = (props) => {
  const { requiredRole, children } = props;
  const { data: user, error } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  const router = useRouter();
  const required = RoleMap.get(requiredRole)!;

  useEffect(() => {
    if (error && error.status === 401)
      router.push(`/account/login?from=${router.asPath}`);

    if (!user?.role) return;

    const current = RoleMap.get(user?.role ?? Role.User)!;

    if (current < required) router.push('/404');
  }, [user, error, required, router]);

  if (!user) {
    return (
      <Center style={{ height: 'calc(100vh - 32px)' }}>
        <Loader/>
      </Center>
    );
  }

  return children;
};

export default WithRole;
