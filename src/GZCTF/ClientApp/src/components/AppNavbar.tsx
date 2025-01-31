import { ActionIcon, AppShell, Avatar, Menu, MenuDivider, Stack, Tooltip, useMantineColorScheme } from '@mantine/core'
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiCached,
  mdiFlagOutline,
  mdiHomeVariantOutline,
  mdiInformationOutline,
  mdiLogin,
  mdiLogout,
  mdiNoteTextOutline,
  mdiPalette,
  mdiTranslate,
  mdiWeatherNight,
  mdiWeatherSunny,
  mdiWrenchOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { LogoBox } from '@Components/LogoBox'
import { AppControlProps } from '@Components/WithNavbar'
import { LanguageMap, SupportedLanguages, useLanguage } from '@Utils/I18n'
import { clearLocalCache } from '@Hooks/useConfig'
import { useLogOut, useUser } from '@Hooks/useUser'
import { Role } from '@Api'
import classes from '@Styles/AppNavbar.module.css'
import misc from '@Styles/Misc.module.css'

interface NavbarItem {
  icon: string
  label: string
  link: string
  admin?: boolean
}

export interface NavbarLinkProps {
  icon: string
  label: string
  link?: string
  onClick?: () => void
  isActive?: boolean
}

const NavbarLink: FC<NavbarLinkProps> = (props: NavbarLinkProps) => {
  const { t } = useTranslation()

  return (
    <Tooltip label={t(props.label)} classNames={classes} position="right">
      <ActionIcon
        onClick={props.onClick}
        component={Link}
        to={props.link ?? '#'}
        data-active={props.isActive || undefined}
        className={classes.link}
      >
        <Icon path={props.icon} size={1} />
      </ActionIcon>
    </Tooltip>
  )
}

export const AppNavbar: FC<AppControlProps> = ({ openColorModal }) => {
  const location = useLocation()
  const { colorScheme, toggleColorScheme } = useMantineColorScheme()

  const logout = useLogOut()
  const { user, error } = useUser()
  const { t } = useTranslation()
  const { setLanguage, supportedLanguages } = useLanguage()

  const items: NavbarItem[] = [
    { icon: mdiHomeVariantOutline, label: 'common.tab.home', link: '/' },
    { icon: mdiNoteTextOutline, label: 'common.tab.post', link: '/posts' },
    { icon: mdiFlagOutline, label: 'common.tab.game', link: '/games' },
    { icon: mdiAccountGroupOutline, label: 'common.tab.team', link: '/teams' },
    { icon: mdiInformationOutline, label: 'common.tab.about', link: '/about' },
    { icon: mdiWrenchOutline, label: 'common.tab.admin', link: '/admin/games', admin: true },
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
    .map((link) => <NavbarLink key={link.label} {...link} isActive={link.label === active} />)

  const loggedIn = user && !error

  return (
    <AppShell.Navbar className={classes.navbar}>
      {/* Logo */}
      <AppShell.Section grow>
        <LogoBox size="100%" className={classes.logo} component={Link} to="/" />
      </AppShell.Section>

      {/* Common Nav */}
      <AppShell.Section className={cx(classes.section, misc.justifyCenter)}>{links}</AppShell.Section>

      <AppShell.Section className={cx(classes.section, misc.justifyEnd)}>
        <Stack w="100%" align="center" justify="center" gap={5}>
          {/* Language */}
          <Menu position="right" offset={24} width={160}>
            <Menu.Target>
              <ActionIcon className={classes.link}>
                <Icon path={mdiTranslate} size={1} />
              </ActionIcon>
            </Menu.Target>

            <Menu.Dropdown>
              {supportedLanguages.map((lang: SupportedLanguages) => (
                <Menu.Item key={lang} fw={500} onClick={() => setLanguage(lang)}>
                  {LanguageMap[lang] ?? lang}
                </Menu.Item>
              ))}
            </Menu.Dropdown>
          </Menu>

          {/* Color Mode */}
          <Tooltip
            label={t('common.tab.theme.switch_to', {
              theme: colorScheme === 'dark' ? t('common.tab.theme.light') : t('common.tab.theme.dark'),
            })}
            classNames={classes}
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

          {/* User Info */}
          <Menu position="right-end" offset={24}>
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
              {loggedIn && (
                <>
                  <Menu.Label>{user?.userName}</Menu.Label>
                  <Menu.Item
                    component={Link}
                    to="/account/profile"
                    leftSection={<Icon path={mdiAccountCircleOutline} size={1} />}
                  >
                    {t('common.tab.account.profile')}
                  </Menu.Item>
                </>
              )}
              <Menu.Item onClick={clearLocalCache} leftSection={<Icon path={mdiCached} size={1} />}>
                {t('common.tab.account.clean_cache')}
              </Menu.Item>
              <Menu.Item onClick={openColorModal} leftSection={<Icon path={mdiPalette} size={1} />}>
                {t('common.content.color.title')}
              </Menu.Item>
              <MenuDivider />
              {loggedIn ? (
                <Menu.Item color="red" onClick={logout} leftSection={<Icon path={mdiLogout} size={1} />}>
                  {t('common.tab.account.logout')}
                </Menu.Item>
              ) : (
                <Menu.Item
                  component={Link}
                  to={`/account/login?from=${location.pathname}`}
                  leftSection={<Icon path={mdiLogin} size={1} />}
                >
                  {t('common.tab.account.login')}
                </Menu.Item>
              )}
            </Menu.Dropdown>
          </Menu>
        </Stack>
      </AppShell.Section>
    </AppShell.Navbar>
  )
}
