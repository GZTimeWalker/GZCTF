import type { NextPage } from 'next';
import { Center } from '@mantine/core';
import WithNavBar from '../components/WithNavbar';
import WithRole from '../components/WithRole';
import { Role } from '../Api';

const Admin: NextPage = () => {
  return (
    <WithNavBar>
      <WithRole requiredRole={Role.Admin} >
        <Center style={{ height: 'calc(100vh - 32px)' }}>
            <h1>Hello Admin!</h1>
        </Center>
      </WithRole>
    </WithNavBar>
  );
};

export default Admin;
