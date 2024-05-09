import { Burger, Group, Menu, useMantineColorScheme, AppShell, ActionIcon } from '@mantine/core'
import { createStyles } from '@mantine/emotion'
import {
  mdiAccountCircleOutline,
  mdiAccountGroupOutline,
  mdiCached,
  mdiLogout,
  mdiTranslate,
  mdiWeatherNight,
  mdiWeatherSunny,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import LogoHeader from '@Components/LogoHeader'
import { LanguageMap, SupportedLanguages, useLanguage } from '@Utils/I18n'
import { useIsMobile } from '@Utils/ThemeOverride'
import { clearLocalCache } from '@Utils/useConfig'
import { useLogOut, useUser } from '@Utils/useUser'

const useHeaderStyles = createStyles((theme, _, u) => ({
  header: {
    width: '100%',
    zIndex: 150,
    border: 'none',
    boxShadow: theme.shadows.md,

    [u.dark]: {
      backgroundColor: theme.colors.gray[8],
    },

    [u.light]: {
      backgroundColor: theme.colors.light[0],
    },
  },
  button: {
    [u.dark]: {
      color: theme.colors.gray[0],
    },

    [u.light]: {
      color: theme.colors.gray[7],
    },
  },
}))

const AppHeader: FC = () => {
  const [opened, setOpened] = useState(false)
  const { classes: headerClasses } = useHeaderStyles()

  const location = useLocation()
  const navigate = useNavigate()

  const { colorScheme, toggleColorScheme } = useMantineColorScheme()
  const { user, error } = useUser()

  const logout = useLogOut()
  const isMobile = useIsMobile()

  const { t } = useTranslation()
  const { setLanguage, supportedLanguages } = useLanguage()

  return (
    <AppShell.Header hidden={!isMobile} h={isMobile ? 60 : 0} className={headerClasses.header}>
      <Group h="100%" p="0 1rem" justify="space-between" wrap="nowrap">
        <LogoHeader onClick={() => navigate('/')} />
        <Group justify="flex-end" wrap="nowrap">
          <Menu position="bottom-end" offset={24} width={160}>
            <Menu.Target>
              <ActionIcon className={headerClasses.button}>
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
          <Menu
            shadow="md"
            opened={opened}
            onClose={() => setOpened(false)}
            width={200}
            offset={13}
          >
            <Menu.Target>
              <Burger opened={opened} onClick={() => setOpened((o) => !o)} />
            </Menu.Target>
            <Menu.Dropdown>
              {user && !error ? (
                <>
                  <Menu.Item
                    component={Link}
                    to="/teams"
                    leftSection={<Icon path={mdiAccountGroupOutline} size={1} />}
                  >
                    {t('common.tab.team')}
                  </Menu.Item>
                  <Menu.Item
                    component={Link}
                    to="/account/profile"
                    leftSection={<Icon path={mdiAccountCircleOutline} size={1} />}
                  >
                    {t('common.tab.account.profile')}
                  </Menu.Item>
                  <Menu.Item
                    onClick={clearLocalCache}
                    leftSection={<Icon path={mdiCached} size={1} />}
                  >
                    {t('common.tab.account.clean_cache')}
                  </Menu.Item>
                  <Menu.Item
                    color="red"
                    onClick={logout}
                    leftSection={<Icon path={mdiLogout} size={1} />}
                  >
                    {t('common.tab.account.logout')}
                  </Menu.Item>
                </>
              ) : (
                <Menu.Item
                  component={Link}
                  to={`/account/login?from=${location.pathname}`}
                  leftSection={<Icon path={mdiAccountCircleOutline} size={1} />}
                >
                  {t('common.tab.account.login')}
                </Menu.Item>
              )}
              <Menu.Divider />
              <Menu.Item
                leftSection={
                  colorScheme === 'dark' ? (
                    <Icon path={mdiWeatherSunny} size={1} />
                  ) : (
                    <Icon path={mdiWeatherNight} size={1} />
                  )
                }
                fw={500}
                onClick={() => toggleColorScheme()}
              >
                {t('common.tab.theme.switch_to', {
                  theme:
                    colorScheme === 'dark'
                      ? t('common.tab.theme.light')
                      : t('common.tab.theme.dark'),
                })}
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </Group>
      </Group>
    </AppShell.Header>
  )
}

export default AppHeader
