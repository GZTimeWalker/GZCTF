import React, { FC, useState } from 'react';
import {
  Navbar,
  Center,
  Stack,
  Tooltip,
  UnstyledButton,
  createStyles,
  Group,
  Title,
  useMantineTheme
} from '@mantine/core';
import {
    mdiAccountCircle,
    mdiAccountGroup,
    mdiChartBox,
    mdiFlag,
    mdiHomeVariant,
    mdiHelp
  } from '@mdi/js';
import { Icon } from '@mdi/react';
import { MainIcon } from './icon/MainIcon';
import Link from 'next/link';

const useStyles = createStyles((theme) => ({
    link: {
        width: 50,
        height: 50,
        borderRadius: theme.radius.md,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[7],

        '&:hover': {
        backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.gray[0],
        },
    },

    active: {
        '&, &:hover': {
        backgroundColor:
            theme.colorScheme === 'dark'
            ? theme.fn.rgba(theme.colors[theme.primaryColor][9], 0.25)
            : theme.colors[theme.primaryColor][0],
        color: theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 7],
        },
    }
}));

const items = [
    { icon: mdiHomeVariant, label: 'Home', link: '/' },
    { icon: mdiAccountGroup, label: 'Teams' },
    { icon: mdiFlag, label: 'Contest' },
    { icon: mdiChartBox, label: 'Scoreboard' },
    { icon: mdiHelp, label: 'About', link: '/about'}
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
        <Tooltip label={props.label} position="right" withArrow transitionDuration={0}>
          <Link href={props.link ?? "#"} passHref>
            <UnstyledButton onClick={props.onClick} className={cx(classes.link, { [classes.active]: props.isActive })}>
                <Icon path={props.icon} size={1} />
            </UnstyledButton>
          </Link>
        </Tooltip>
    )
}

export const AppNavbar: FC = () => {

    const [active, setActive] = useState(2);
    const theme = useMantineTheme();

    const links = items.map((link, index) => (
      <NavbarLink
        {...link}
        key={link.label}
        isActive={index === active}
        onClick={() => { setActive(index); }}
        />
    ));

    return (
        <Navbar fixed width={{ base: 80 }} p="md">
          <Center>
            <Stack>
              <MainIcon style={{ width: "100%", height: "auto" }}/>
              <Title style={{
                  color: "#fff",
                  fontWeight: "normal",
                  writingMode: "vertical-rl",
                  fontFamily: "'Chakra Petch', sans-serif",
                }}>GZ<span style={{position: "relative", left: ".3rem", color: theme.colors.brand[4]}}>::</span>CTF</Title>
            </Stack>
          </Center>
        <Navbar.Section grow mb={100} style={{display: "flex", alignItems:"center"}}>
          <Group direction="column" align="center" spacing={0}>
            {links}
          </Group>
        </Navbar.Section>
        <Navbar.Section>
          <Group direction="column" align="center" spacing={0}>
            <NavbarLink icon={mdiAccountCircle} label="Account" />
          </Group>
        </Navbar.Section>
      </Navbar >
    )
}
