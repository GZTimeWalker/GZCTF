import React, { FC, useEffect, useState } from 'react';
import {
  Box,
  Stack,
  Center,
  Navbar,
  createStyles,
  UnstyledButton,
  useMantineColorScheme,
} from '@mantine/core';
import {
  mdiAccountCircle,
  mdiAccountGroup,
  mdiChartBox,
  mdiFlag,
  mdiHomeVariant,
  mdiHelp,
  mdiWhiteBalanceSunny,
  mdiWeatherNight,
} from '@mdi/js';
import { Icon } from '@mdi/react';
import MainIcon from './icon/MainIcon';
import Link from 'next/link';
import { useRouter } from 'next/router';

const useStyles = createStyles((theme) => ({
  link: {
    width: 40,
    height: 40,
    borderRadius: theme.radius.md,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[3],
    cursor: 'pointer',

    '&:hover': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.gray[6],
    },
  },

  active: {
    '&, &:hover': {
      backgroundColor:
        theme.colorScheme === 'dark'
          ? theme.fn.rgba(theme.colors[theme.primaryColor][7], 0.25)
          : theme.fn.rgba(theme.colors[theme.primaryColor][0], 0.25),
      color: theme.colors[theme.primaryColor][4],
    },
  },

  navbar: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.colors.gray[7],
  },
}));

const items = [
  { icon: mdiHomeVariant, label: 'Home', link: '/' },
  { icon: mdiAccountGroup, label: 'Teams' },
  { icon: mdiFlag, label: 'Contest' },
  { icon: mdiChartBox, label: 'Scoreboard' },
  { icon: mdiHelp, label: 'About', link: '/about' },
];

export interface NavbarLinkProps {
  icon: string;
  label?: string;
  link?: string;
  onClick?: () => void;
  isActive?: boolean;
}

const NavbarLink: FC<NavbarLinkProps> = (props: NavbarLinkProps) => {
  const { classes, cx } = useStyles();
  return (
    <Link href={props.link ?? '#'} passHref>
      <UnstyledButton
        onClick={props.onClick}
        className={cx(classes.link, { [classes.active]: props.isActive })}
      >
        <Icon path={props.icon} size={1} />
      </UnstyledButton>
    </Link>
  );
};

const AppNavbar: FC = () => {
  const router = useRouter();
  const { classes, cx } = useStyles();
  const [active, setActive] = useState('Home');
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();

  useEffect(() => {
    items.forEach((i) => {
      if (i.link && router.pathname.startsWith(i.link)) {
        setActive(i.label);
      }
    });
  }, [router.pathname]);

  const links = items.map((link) => (
    <NavbarLink {...link} key={link.label} isActive={link.label === active} />
  ));

  return (
    <Navbar fixed width={{ base: 70 }} p="md" className={classes.navbar}>
      <Center>
        <MainIcon style={{ width: '100%', height: 'auto' }} ignoreTheme/>
      </Center>
      <Navbar.Section grow mb={100} style={{ display: 'flex', alignItems: 'center' }}>
        <Stack align="center" spacing={5}>
          {links}
        </Stack>
      </Navbar.Section>
      <Navbar.Section>
        <Stack align="center" spacing={5}>
          <UnstyledButton onClick={() => toggleColorScheme()} className={cx(classes.link)}>
            {colorScheme === 'dark' ? (
              <Icon path={mdiWhiteBalanceSunny} size={1} />
            ) : (
              <Icon path={mdiWeatherNight} size={1} />
            )}
          </UnstyledButton>
          {/* TODO: /profile but redirect to login when there is no user */}
          <Link href="/login" passHref>
            <Box className={cx(classes.link)}>
              <Icon path={mdiAccountCircle} size={1} />
            </Box>
          </Link>
        </Stack>
      </Navbar.Section>
    </Navbar>
  );
};

export default AppNavbar;
