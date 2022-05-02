import React, { FC, useEffect, useState } from 'react';
import {
  Box,
  Stack,
  Center,
  Navbar,
  createStyles,
  UnstyledButton,
  useMantineColorScheme,
  Tooltip,
} from '@mantine/core';
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiChartBoxOutline,
  mdiFlagOutline,
  mdiHomeVariantOutline,
  mdiInformationOutline,
  mdiWeatherSunny,
  mdiWeatherNight,
} from '@mdi/js';
import { Icon } from '@mdi/react';
import MainIcon from './icon/MainIcon';
import Link from 'next/link';
import { useRouter } from 'next/router';
import useSWR from 'swr';
import { AccountService, ClientUserInfoModel } from '../client';

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
      backgroundColor: theme.colors.gray[6] + "80",
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
    backgroundColor: theme.colorScheme === 'dark'
      ? theme.colors[theme.primaryColor][8] + "40"
      : theme.colors[theme.primaryColor][2],
    color: theme.colorScheme === 'dark'
      ? theme.colors[theme.primaryColor][4]
      : theme.colors.gray[8]
  }
}));

const items = [
  { icon: mdiHomeVariantOutline, label: 'Home', link: '/' },
  { icon: mdiAccountGroupOutline, label: 'Teams' },
  { icon: mdiFlagOutline, label: 'Contest' },
  { icon: mdiChartBoxOutline, label: 'Scoreboard' },
  { icon: mdiInformationOutline, label: 'About', link: '/about' },
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
      <Tooltip
        label={props.label}
        classNames={{ body: classes.tooltipBody }}
        position="right"
      >
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
  const [active, setActive] = useState('Home');
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const { data } = useSWR<ClientUserInfoModel>('/api/account/profile');

  useEffect(() => {
    items.forEach((i) => {
      if (i.link && router.pathname.startsWith(i.link)) {
        setActive(i.label);
      }
    });
  }, [router.pathname]);

  console.log(data)

  const links = items.map((link) => (
    <NavbarLink {...link} key={link.label} isActive={link.label === active} />
  ));

  return (
    <Navbar fixed width={{ base: 70 }} p="md" className={classes.navbar}>
      <Navbar.Section grow>
        <Center>
          <MainIcon style={{ width: "100%", height: 'auto', position: "relative", left: 2 }} ignoreTheme />
        </Center>
      </Navbar.Section>
      <Navbar.Section grow mb={20} mt={20} style={{ display: 'flex', alignItems: 'center' }}>
        <Stack align="center" spacing={5}>
          {links}
        </Stack>
      </Navbar.Section>
      <Navbar.Section grow style={{ display: "flex", flexDirection: "column", justifyContent: "end" }}>
        <Stack align="center" spacing={5}>
          <Tooltip
            label={colorScheme === 'dark' ? "Light Theme" : "Dark Theme"}
            classNames={{ body: classes.tooltipBody }}
            position="right"
          >
            <UnstyledButton onClick={() => toggleColorScheme()} className={cx(classes.link)}>
              {colorScheme === 'dark'
                ? <Icon path={mdiWeatherSunny} size={1} />
                : <Icon path={mdiWeatherNight} size={1} />
              }
            </UnstyledButton>
          </Tooltip>
          {/* TODO: /profile but redirect to login when there is no user */}
          <Link href={`/account/login?from=${router.asPath}`} passHref>
            <Tooltip
              label="Login"
              classNames={{ body: classes.tooltipBody }}
              position="right"
            >
              <Box className={cx(classes.link)}>
                <Icon path={mdiAccountCircleOutline} size={1} />
              </Box>
            </Tooltip>
          </Link>
        </Stack>
      </Navbar.Section>
    </Navbar>
  );
};

export default AppNavbar;
