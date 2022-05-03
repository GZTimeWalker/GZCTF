import type { NextPage } from 'next';
import { Stack, Card, Group, Badge, Text, useMantineTheme } from '@mantine/core';
import api from '../Api';
import LogoHeader from '../components/LogoHeader';
import WithNavBar from '../components/WithNavbar';

const Home: NextPage = () => {
  const { data } = api.info.useInfoGetNotices();
  const theme = useMantineTheme();
  const secondaryColor = theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7];

  return (
    <WithNavBar>
      <Stack align="center">
        <LogoHeader />
        <Card shadow="sm" p="lg" style={{ width: '80%' }}>
          <Group position="apart" style={{ margin: 'auto' }}>
            <Text weight={500}>{data && data.length > 0 ? data[0].title : 'Welcome!'}</Text>
            <Badge color="brand" variant="light">
              {data && data.length > 0
                ? new Date(data[0].time).toLocaleString()
                : new Date().toLocaleString()}
            </Badge>
          </Group>
          <Text size="sm" style={{ color: secondaryColor, lineHeight: 1.5 }}>
            {data && data.length > 0
              ? data[0].content
              : 'Lorem ipsum dolor sit amet consectetur adipisicing elit. Amet nam esse placeat, ipsa commodi aliquam aperiam quaerat quam velit exercitationem voluptates quidem ut eius ducimus aliquid est magni eaque corrupti?'}
          </Text>
        </Card>
      </Stack>
    </WithNavBar>
  );
};

export default Home;
