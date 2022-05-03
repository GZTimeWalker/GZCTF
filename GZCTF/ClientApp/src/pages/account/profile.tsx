import type { NextPage } from 'next';
import { Text, Stack, Tabs } from '@mantine/core';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';

const Profile: NextPage = () => {
  const { data } = api.account.useAccountProfile();

  return (
    <WithNavBar>
      <Tabs color="brand">
        <Tabs.Tab label="基础信息"></Tabs.Tab>
        <Tabs.Tab label="更改密码"></Tabs.Tab>
        <Tabs.Tab label="更改邮箱"></Tabs.Tab>
      </Tabs>
    </WithNavBar>
  );
};

export default Profile;
