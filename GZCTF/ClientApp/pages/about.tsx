import type { NextPage } from 'next'
import { Text, Stack, Group, Title  } from '@mantine/core';
import { MainIcon } from './components/icon/MainIcon';
import { WithNavBar } from './components/WithNavbar';

const About: NextPage = () => {
  return (
    <WithNavBar>
      <Stack>
        <Group>
            <MainIcon style={{ width: 200}}/>
            <Title>GZ::CTF</Title>
        </Group>
        <Text>
            Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur soluta fuga explicabo. Perferendis culpa atque quisquam soluta hic molestias similique quas id. Quae incidunt possimus sit minus veniam, repellat officia. Lorem ipsum dolor sit amet consectetur, adipisicing elit. Itaque molestias dolorem unde minima atque earum libero velit consequuntur! Quibusdam at rem laborum exercitationem, incidunt quas possimus porro. Quibusdam, enim expedita.
        </Text>
      </Stack>
    </WithNavBar>
  )
}

export default About;
