import type { NextPage } from 'next';
import { Center } from '@mantine/core';
import WithNavBar from '../components/WithNavbar';
import Icon404 from "../components/icon/404Icon";


const Error404: NextPage = () => {

  return (
    <WithNavBar>
      <Center style={{ height: "calc(100vh - 32px)" }}>
        <Icon404 />
      </Center>
    </WithNavBar >
  );
};

export default Error404;
