import {
  ActionIcon,
  Avatar,
  Center,
  createStyles,
  getStylesRef,
  Menu,
  Navbar,
  Stack,
  Tooltip,
  useMantineColorScheme,
} from '@mantine/core'
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiCached,
  mdiFlagOutline,
  mdiHomeVariantOutline,
  mdiInformationOutline,
  mdiLogout,
  mdiNoteTextOutline,
  mdiSignLanguage,
  mdiWeatherNight,
  mdiWeatherSunny,
  mdiWrenchOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import MainIcon from '@Components/icon/MainIcon'
import { useLanguage } from '@Utils/I18n'
import { useLocalStorageCache } from '@Utils/useConfig'
import { useLoginOut, useUser } from '@Utils/useUser'
import { Role } from '@Api'

const useStyles = createStyles((theme) => {
  const active = { ref: getStylesRef('activeItem') } as const

  return {
    active,
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

      [`&.${active.ref}, &.${active.ref}:hover`]: {
        backgroundColor: theme.fn.rgba(theme.colors[theme.primaryColor][7], 0.25),
        color: theme.colors[theme.primaryColor][4],
      },
    },

    navbar: {
      backgroundColor: theme.colors.gray[8],

      [theme.fn.smallerThan('xs')]: {
        display: 'none',
      },
    },

    tooltipBody: {
      marginLeft: 20,
      backgroundColor:
        theme.colorScheme === 'dark'
          ? theme.fn.darken(theme.colors[theme.primaryColor][8], 0.45)
          : theme.colors[theme.primaryColor][6],
      color:
        theme.colorScheme === 'dark' ? theme.colors[theme.primaryColor][4] : theme.colors.white[0],
    },

    menuBody: {
      left: 100,
    },
  }
})

interface NavbarItem {
  icon: string
  label: string
  link: string
  admin?: boolean
}

export interface NavbarLinkProps {
  icon: string
  label?: string
  link?: string
  onClick?: () => void
  isActive?: boolean
}

const NavbarLink: FC<NavbarLinkProps> = (props: NavbarLinkProps) => {
  const { classes, cx } = useStyles()

  return (
    <Tooltip label={props.label} classNames={{ tooltip: classes.tooltipBody }} position="right">
      <ActionIcon
        onClick={props.onClick}
        component={Link}
        to={props.link ?? '#'}
        className={cx(classes.link, { [classes.active]: props.isActive })}
      >
        <Icon path={props.icon} size={1} />
      </ActionIcon>
    </Tooltip>
  )
}

const AppNavbar: FC = () => {
  const location = useLocation()
  const navigate = useNavigate()
  const { classes } = useStyles()
  const { colorScheme, toggleColorScheme } = useMantineColorScheme()

  const logout = useLoginOut()
  const { clearLocalCache } = useLocalStorageCache()
  const { user, error } = useUser()
  const { t } = useTranslation()
  const { language, setLanguage, supportedLanguages } = useLanguage()

  const items: NavbarItem[] = [
    { icon: mdiHomeVariantOutline, label: t('common.tab.home'), link: '/' },
    { icon: mdiNoteTextOutline, label: t('common.tab.post'), link: '/posts' },
    { icon: mdiFlagOutline, label: t('common.tab.game'), link: '/games' },
    { icon: mdiAccountGroupOutline, label: t('common.tab.team'), link: '/teams' },
    { icon: mdiInformationOutline, label: t('common.tab.about'), link: '/about' },
    { icon: mdiWrenchOutline, label: t('common.tab.manage'), link: '/admin/games', admin: true },
  ]

  const getLabel = (path: string) =>
    items.find((item) =>
      item.link === '/'
        ? path === '/'
        : item.link.startsWith('/admin')
          ? path.startsWith('/admin')
          : path.startsWith(item.link)
    )?.label

  const [active, setActive] = useState(getLabel(location.pathname) ?? '')

  useEffect(() => {
    if (location.pathname === '/') {
      setActive(items[0].label)
    } else {
      setActive(getLabel(location.pathname) ?? '')
    }
  }, [location.pathname])

  const links = items
    .filter((m) => !m.admin || user?.role === Role.Admin)
    .map((link) => <NavbarLink {...link} key={link.label} isActive={link.label === active} />)

  return (
    <Navbar fixed width={{ xs: 70, base: 0 }} p="md" className={classes.navbar}>
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
            label={t('common.tab.theme.switch_to', {
              theme:
                colorScheme === 'dark' ? t('common.tab.theme.light') : t('common.tab.theme.dark'),
            })}
            classNames={{ tooltip: classes.tooltipBody }}
            position="right"
          >
            <ActionIcon onClick={() => toggleColorScheme()} className={classes.link}>
              {colorScheme === 'dark' ? (
                <Icon path={mdiWeatherSunny} size={1} />
              ) : (
                <Icon path={mdiWeatherNight} size={1} />
              )}
            </ActionIcon>
          </Tooltip>

          {/* Language */}
          <Menu position="right-end" offset={24} width={160}>
            <Menu.Target>
              <ActionIcon className={classes.link}>
                <Icon path={mdiSignLanguage} size={1} />
              </ActionIcon>
            </Menu.Target>

            <Menu.Dropdown>
              {supportedLanguages.map((lang) => (
                <Menu.Item
                  key={lang}
                  onClick={() => setLanguage(lang)}
                  icon={<Icon path={mdiSignLanguage} size={1} />}
                >
                  {lang}
                </Menu.Item>
              ))}
            </Menu.Dropdown>
          </Menu>

          {/* User Info */}
          {user && !error ? (
            <Menu position="right-end" offset={24} width={160}>
              <Menu.Target>
                <ActionIcon className={classes.link}>
                  {user?.avatar ? (
                    <Avatar alt="avatar" src={user?.avatar} radius="md" size="md">
                      {user.userName?.slice(0, 1) ?? 'U'}
                    </Avatar>
                  ) : (
                    <Icon path={mdiAccountCircleOutline} size={1} />
                  )}
                </ActionIcon>
              </Menu.Target>

              <Menu.Dropdown>
                <Menu.Label>{user.userName}</Menu.Label>
                <Menu.Item
                  component={Link}
                  to="/account/profile"
                  icon={<Icon path={mdiAccountCircleOutline} size={1} />}
                >
                  {t('common.tab.account.profile')}
                </Menu.Item>
                <Menu.Item onClick={clearLocalCache} icon={<Icon path={mdiCached} size={1} />}>
                  {t('common.tab.account.clean_cache')}
                </Menu.Item>
                <Menu.Item color="red" onClick={logout} icon={<Icon path={mdiLogout} size={1} />}>
                  {t('common.tab.account.logout')}
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          ) : (
            <Tooltip
              label={t('common.tab.account.login')}
              classNames={{ tooltip: classes.tooltipBody }}
              position="right"
            >
              <ActionIcon
                component={Link}
                to={`/account/login?from=${location.pathname}`}
                className={classes.link}
              >
                <Icon path={mdiAccountCircleOutline} size={1} />
              </ActionIcon>
            </Tooltip>
          )}
        </Stack>
      </Navbar.Section>
    </Navbar>
  )
}

export default AppNavbar
