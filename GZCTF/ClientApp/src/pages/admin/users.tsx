import type { NextPage } from 'next';
import { Center } from '@mantine/core';
import WithNavBar from '../../components/WithNavbar';
import WithRole from '../../components/WithRole';
import { Role } from '../../Api';

const UserManager: NextPage = () => {
  return (
    <WithNavBar>
      <WithRole requiredRole={Role.Admin} >
        <Center style={{ height: 'calc(100vh - 32px)' }}>
            <h1>User Manager</h1>
        </Center>
      </WithRole>
    </WithNavBar>
  );
};

export default UserManager;
