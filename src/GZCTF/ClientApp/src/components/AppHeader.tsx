import { Burger, createStyles, Group, Header, Menu, useMantineColorScheme } from '@mantine/core'
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiCached,
  mdiLogout,
  mdiWeatherNight,
  mdiWeatherSunny,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import LogoHeader from '@Components/LogoHeader'
import { useIsMobile } from '@Utils/ThemeOverride'
import { clearLocalCache } from '@Utils/useConfig'
import { useLoginOut, useUser } from '@Utils/useUser'

const useHeaderStyles = createStyles((theme) => ({
  header: {
    width: '100%',
    zIndex: 150,
    backgroundColor: colorScheme === 'dark' ? theme.colors.gray[8] : theme.colors.white[0],
    border: 'none',
    boxShadow: theme.shadows.md,
  },
}))

const AppHeader: FC = () => {
  const [opened, setOpened] = useState(false)
  const { classes: headerClasses } = useHeaderStyles()

  const location = useLocation()
  const navigate = useNavigate()

  const { colorScheme, toggleColorScheme } = useMantineColorScheme()
  const { user, error } = useUser()

  const logout = useLoginOut()
  const isMobile = useIsMobile()

  const { t } = useTranslation()

  return (
    <Header fixed hidden={!isMobile} height={isMobile ? 60 : 0} className={headerClasses.header}>
      <Group h="100%" p="0 1rem" justify="space-between" wrap="nowrap">
        <LogoHeader onClick={() => navigate('/')} />
        <Menu shadow="md" opened={opened} onClose={() => setOpened(false)} width={200} offset={13}>
          <Menu.Target>
            <Burger opened={opened} onClick={() => setOpened((o) => !o)} />
          </Menu.Target>
          <Menu.Dropdown>
            {user && !error ? (
              <>
                <Menu.Item
                  component={Link}
                  to="/teams"
                  icon={<Icon path={mdiAccountGroupOutline} size={1} />}
                >
                  {t('common.tab.team')}
                </Menu.Item>
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
              </>
            ) : (
              <Menu.Item
                component={Link}
                to={`/account/login?from=${location.pathname}`}
                icon={<Icon path={mdiAccountCircleOutline} size={1} />}
              >
                {t('common.tab.account.login')}
              </Menu.Item>
            )}
            <Menu.Divider />
            <Menu.Item
              icon={
                colorScheme === 'dark' ? (
                  <Icon path={mdiWeatherSunny} size={1} />
                ) : (
                  <Icon path={mdiWeatherNight} size={1} />
                )
              }
              onClick={() => toggleColorScheme()}
            >
              {t('common.tab.theme.switch_to', {
                theme:
                  colorScheme === 'dark' ? t('common.tab.theme.light') : t('common.tab.theme.dark'),
              })}
            </Menu.Item>
          </Menu.Dropdown>
        </Menu>
      </Group>
    </Header>
  )
}

export default AppHeader
