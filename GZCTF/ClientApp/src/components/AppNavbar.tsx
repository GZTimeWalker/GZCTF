import React, { FC, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
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
  mdiWrenchOutline,
} from '@mdi/js';
import { Icon } from '@mdi/react';
import api, { Role } from '../Api';
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
        ? theme.fn.darken(theme.colors[theme.primaryColor][8], 0.45)
        : theme.colors[theme.primaryColor][2],
    color:
      theme.colorScheme === 'dark' ? theme.colors[theme.primaryColor][4] : theme.colors.gray[8],
  },

  menuBody: {
    marginLeft: 20,
  },
}));

interface NavbarItem {
  icon: string;
  label: string;
  link: string;
  admin?: boolean;
}

const items: NavbarItem[] = [
  { icon: mdiHomeVariantOutline, label: '主页', link: '/' },
  { icon: mdiFlagOutline, label: '赛事', link: '/games' },
  { icon: mdiAccountGroupOutline, label: '队伍', link: '/teams' },
  { icon: mdiInformationOutline, label: '关于', link: '/about' },
  { icon: mdiWrenchOutline, label: '管理', link: '/admin', admin: true },
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
    <Link to={props.link ?? '#'}>
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
  const location = useLocation();
  const navigate = useNavigate();
  const { classes, cx } = useStyles();
  const [active, setActive] = useState('');
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();

  const { data: user, error } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  useEffect(() => {
    if (location.pathname == '/') {
      setActive(items[0].label);
    }
    items.forEach((i) => {
      if (location.pathname.startsWith(i.link) && i.link != '/') {
        setActive(i.label);
      }
    });
  }, [location.pathname]);

  const logout = () => {
    api.account.accountLogOut().then(() => {
      navigate('/');
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

  const links = items
    .filter((m) => !m.admin || user?.role === Role.Admin)
    .map((link) => <NavbarLink {...link} key={link.label} isActive={link.label === active} />);

  return (
    <Navbar fixed width={{ base: 70 }} p="md" className={classes.navbar}>
      {/* Logo */}
      <Navbar.Section grow>
        <Center>
          <MainIcon
            style={{ width: '100%', height: 'auto', position: 'relative', left: 2 }}
            ignoreTheme
            onClick={() => navigate('/')}
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
          <Tooltip
            label={'切换至' + (colorScheme === 'dark' ? '浅色' : '深色') + '主题'}
            classNames={{ body: classes.tooltipBody }}
            position="right"
          >
            <UnstyledButton onClick={() => toggleColorScheme()} className={cx(classes.link)}>
              {colorScheme === 'dark' ? (
                <Icon path={mdiWeatherSunny} size={1} />
              ) : (
                <Icon path={mdiWeatherNight} size={1} />
              )}
            </UnstyledButton>
          </Tooltip>

          {/* User Info */}
          {user && !error ? (
            <Menu
              control={
                <Tooltip label="账户" classNames={{ body: classes.tooltipBody }} position="right">
                  <Box className={cx(classes.link)}>
                    {user.avatar ? (
                      <Avatar src={user.avatar} radius="md" size="md" />
                    ) : (
                      <Icon path={mdiAccountCircleOutline} size={1} />
                    )}
                  </Box>
                </Tooltip>
              }
              classNames={{ body: classes.menuBody }}
              position="right"
              placement="end"
              trigger="click"
            >
              <Menu.Label>{user.userName}</Menu.Label>
              <Menu.Item
                component={Link}
                to="/account/profile"
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
            <Link to={`/account/login?from=${location.pathname}`}>
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
