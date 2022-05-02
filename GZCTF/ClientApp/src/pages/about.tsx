import type { NextPage } from 'next';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import MainIcon from '../components/icon/MainIcon';
import WithNavBar from '../components/WithNavbar';
import RichTextEditor from '../components/RichText';
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
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Minima nostrum soluta vitae ea nulla cum autem possimus dicta provident aliquam. Quibusdam, consequatur eligendi dolorum illo impedit quos dolores aut fuga.
        </Text>
        <Text size='xs'>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur soluta fuga explicabo.
          Perferendis culpa atque quisquam soluta hic molestias similique quas id. Quae incidunt
          possimus sit minus veniam, repellat officia.
        </Text>
        <Text size='sm'>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur magni illum assumenda perspiciatis, dicta quam tempore sit dolorem, repellendus necessitatibus distinctio consequuntur ad ipsa, excepturi eligendi aut vel voluptas quidem.
        </Text>
        <Text size='md'>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Neque, assumenda consequuntur? Tempora doloremque provident sit praesentium quo, repellat eligendi expedita saepe minus aspernatur nobis vero sint error perferendis iure nulla.
        </Text>
        <Text size='lg'>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aperiam aspernatur molestias, blanditiis nobis sed eum. Culpa aspernatur earum blanditiis deleniti, dolor repellat vero facere recusandae deserunt quod quidem, eos magni!
        </Text>
        <Text size='xl'>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Quae dolorem sunt soluta necessitatibus. Consequatur nulla minus, perspiciatis sint, ab aliquam ipsam magni illum ipsum, voluptatem exercitationem expedita doloribus. Error, ad?
        </Text>
        <Center>
          <RichTextEditor style={{ width: '95%', height: 500 }} value={value} onChange={onChange} />
        </Center>
      </Stack>
    </WithNavBar>
  );
};

export default About;
