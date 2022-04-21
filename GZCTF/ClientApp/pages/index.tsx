import type { NextPage } from 'next';
import Link from 'next/link';
import { Button, Stack, Group  } from '@mantine/core';
import { MainIcon } from './components/icon/MainIcon';
import { WithNavBar } from './components/WithNavbar';

const Home: NextPage = () => {
  return (
    <WithNavBar>
      <Stack align="center">
        <MainIcon style={{ maxWidth: 300 }}/>
        <Group>
          <Link href="/" passHref>
            <Button component="a" color="brand" variant="outline"  > Home </Button>
          </Link>
          <Link href="/about" passHref>
            <Button component="a" color="brand" variant="outline"  > About </Button>
          </Link>
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default Home;
