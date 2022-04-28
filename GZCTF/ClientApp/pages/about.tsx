import type { NextPage } from 'next';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import { MainIcon } from './components/icon/MainIcon';
import { WithNavBar } from './components/WithNavbar';
import RichTextEditor from './components/RichText';
import { useState } from 'react';

const initialValue =
  '<p>Your initial <b>html value</b> or an empty string to init editor without value</p>';

const About: NextPage = () => {
  const [value, onChange] = useState(initialValue);

  return (
    <WithNavBar>
      <Stack>
        <Group>
          <MainIcon style={{ height: 200, width: "auto" }} />
          <Title>GZ::CTF</Title>
        </Group>
        <Text>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur soluta fuga explicabo.
          Perferendis culpa atque quisquam soluta hic molestias similique quas id. Quae incidunt
          possimus sit minus veniam, repellat officia. Lorem ipsum dolor sit amet consectetur,
          adipisicing elit. Itaque molestias dolorem unde minima atque earum libero velit
          consequuntur! Quibusdam at rem laborum exercitationem, incidunt quas possimus porro.
          Quibusdam, enim expedita.
        </Text>
        <Center>
          <RichTextEditor style={{ width: '95%', height: 500 }} value={value} onChange={onChange} />
        </Center>
      </Stack>
    </WithNavBar>
  );
};

export default About;
