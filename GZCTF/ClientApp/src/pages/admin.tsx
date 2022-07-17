import type { NextPage } from 'next';
import { Center } from '@mantine/core';
import { Role } from '../Api';
import WithNavBar from '../components/WithNavbar';
import WithRole from '../components/WithRole';

const Admin: NextPage = () => {
  return (
    <WithNavBar>
      <WithRole requiredRole={Role.Admin}>
        <Center style={{ height: 'calc(100vh - 32px)' }}>
          <h1>Hello Admin!</h1>
        </Center>
      </WithRole>
    </WithNavBar>
  );
};

export default Admin;
