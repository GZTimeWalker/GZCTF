import React, { FC, useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/router';
import {
  Box,
  Menu,
  Stack,
  Center,
  Navbar,
  Avatar,
  Divider,
  Tooltip,
  createStyles,
  UnstyledButton,
  useMantineColorScheme,
} from '@mantine/core';
import { NextLink } from '@mantine/next';
import { showNotification } from '@mantine/notifications';
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiFlagOutline,
  mdiHomeVariantOutline,
  mdiInformationOutline,
  mdiWeatherSunny,
  mdiWeatherNight,
  mdiLogout,
  mdiCheck,
} from '@mdi/js';
import { Icon } from '@mdi/react';
import api from '../Api';
import MainIcon from './icon/MainIcon';

const useStyles = createStyles((theme) => ({
  link: {
    width: 40,
    height: 40,
    borderRadius: theme.radius.md,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: theme.colors.gray[1],
    cursor: 'pointer',

    '&:hover': {
      backgroundColor: theme.colors.gray[6] + '80',
    },
  },

  active: {
    '&, &:hover': {
      backgroundColor: theme.fn.rgba(theme.colors[theme.primaryColor][7], 0.25),
      color: theme.colors[theme.primaryColor][4],
    },
  },

  navbar: {
    backgroundColor: theme.colors.gray[8],
  },

  tooltipBody: {
    marginLeft: 20,
    backgroundColor:
      theme.colorScheme === 'dark'
        ? theme.colors[theme.primaryColor][8] + '40'
        : theme.colors[theme.primaryColor][2],
    color:
      theme.colorScheme === 'dark' ? theme.colors[theme.primaryColor][4] : theme.colors.gray[8],
  },

  menuBody: {
    marginLeft: 20,
  },
}));

const items = [
  { icon: mdiHomeVariantOutline, label: '主页', link: '/' },
  { icon: mdiFlagOutline, label: '赛事', link: '/games' },
  { icon: mdiAccountGroupOutline, label: '队伍', link: '/teams' },
  { icon: mdiInformationOutline, label: '关于', link: '/about' },
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
      <Tooltip label={props.label} classNames={{ body: classes.tooltipBody }} position="right">
        <UnstyledButton
          onClick={props.onClick}
          className={cx(classes.link, { [classes.active]: props.isActive })}
        >
          <Icon path={props.icon} size={1} />
        </UnstyledButton>
      </Tooltip>
    </Link>
  );
};

const AppNavbar: FC = () => {
  const router = useRouter();
  const { classes, cx } = useStyles();
  const [active, setActive] = useState('');
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const { data, error } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  useEffect(() => {
    if (router.pathname == '/') {
      setActive(items[0].label);
    }
    items.forEach((i) => {
      if (router.pathname.startsWith(i.link) && i.link != '/') {
        setActive(i.label);
      }
    });
  }, [router.pathname]);

  const logout = () => {
    api.account.accountLogOut().then(() => {
      router.push('/');
      api.account.mutateAccountProfile();
      showNotification({
        color: 'teal',
        title: '登出成功',
        message: '',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      });
    });
  };

  const links = items.map((link) => (
    <NavbarLink {...link} key={link.label} isActive={link.label === active} />
  ));

  return (
    <Navbar fixed width={{ base: 70 }} p="md" className={classes.navbar}>
      {/* Logo */}
      <Navbar.Section grow>
        <Center>
          <MainIcon
            style={{ width: '100%', height: 'auto', position: 'relative', left: 2 }}
            ignoreTheme
            onClick={() => router.push('/')}
          />
        </Center>
      </Navbar.Section>

      {/* Common Nav */}
      <Navbar.Section grow mb={20} mt={20} style={{ display: 'flex', alignItems: 'center' }}>
        <Stack align="center" spacing={5}>
          {links}
        </Stack>
      </Navbar.Section>

      <Navbar.Section
        grow
        style={{ display: 'flex', flexDirection: 'column', justifyContent: 'end' }}
      >
        <Stack align="center" spacing={5}>
          
          {/* Color Mode */}
          <UnstyledButton onClick={() => toggleColorScheme()} className={cx(classes.link)}>
            {colorScheme === 'dark' ? (
              <Icon path={mdiWeatherSunny} size={1} />
            ) : (
              <Icon path={mdiWeatherNight} size={1} />
            )}
          </UnstyledButton>

          {/* User Info */}
          {data && !error ? (
            <Menu
              control={
                <Box className={cx(classes.link)}>
                  {data.avatar ? (
                    <Avatar src={data.avatar} radius="md" size="md" />
                  ) : (
                    <Icon path={mdiAccountCircleOutline} size={1} />
                  )}
                </Box>
              }
              classNames={{ body: classes.menuBody }}
              position="right"
              placement="end"
              trigger="hover"
            >
              <Menu.Label>{data.userName}</Menu.Label>
              <Menu.Item
                component={NextLink}
                href="/account/profile"
                icon={<Icon path={mdiAccountCircleOutline} size={1} />}
              >
                用户信息
              </Menu.Item>

              <Divider />

              <Menu.Item color="red" onClick={logout} icon={<Icon path={mdiLogout} size={1} />}>
                登出
              </Menu.Item>
            </Menu>
          ) : (
            <Link href={`/account/login?from=${router.asPath}`} passHref>
              <Tooltip label="登录" classNames={{ body: classes.tooltipBody }} position="right">
                <Box className={cx(classes.link)}>
                  <Icon path={mdiAccountCircleOutline} size={1} />
                </Box>
              </Tooltip>
            </Link>
          )}
        </Stack>
      </Navbar.Section>
    </Navbar>
  );
};

export default AppNavbar;
