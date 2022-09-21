import { FC, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Burger, createStyles, Group, Header, Menu, useMantineColorScheme } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiWeatherSunny,
  mdiWeatherNight,
  mdiAccountCircleOutline,
  mdiLogout,
  mdiCheck,
  mdiAccountGroupOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useUser } from '@Utils/useUser'
import api from '@Api'
import LogoHeader from './LogoHeader'

const useHeaderStyles = createStyles((theme) => ({
  header: {
    width: '100%',
    zIndex: 500,
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.gray[8] : theme.colors.white[0],
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

  const logout = () => {
    api.account.accountLogOut().then(() => {
      navigate('/')
      api.account.mutateAccountProfile()
      showNotification({
        color: 'teal',
        title: '登出成功',
        message: '',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
    })
  }

  const { isMobile } = useIsMobile()

  return (
    <Header fixed hidden={!isMobile} height={isMobile ? 60 : 0} className={headerClasses.header}>
      <Group style={{ height: '100%' }} p="0 1rem" position="apart">
        <LogoHeader onClick={() => navigate('/')} />
        <Menu
          shadow="md"
          opened={opened}
          onChange={() => setOpened((o) => !o)}
          width={200}
          offset={13}
        >
          <Menu.Target>
            <Burger opened={opened} />
          </Menu.Target>

          <Menu.Dropdown>
            {user && !error ? (
              <>
                <Menu.Item
                  component={Link}
                  to="/teams"
                  icon={<Icon path={mdiAccountGroupOutline} size={1} />}
                >
                  队伍管理
                </Menu.Item>
                <Menu.Item
                  component={Link}
                  to="/account/profile"
                  icon={<Icon path={mdiAccountCircleOutline} size={1} />}
                >
                  用户信息
                </Menu.Item>
                <Menu.Item color="red" onClick={logout} icon={<Icon path={mdiLogout} size={1} />}>
                  登出
                </Menu.Item>
              </>
            ) : (
              <Menu.Item
                component={Link}
                to={`/account/login?from=${location.pathname}`}
                icon={<Icon path={mdiAccountCircleOutline} size={1} />}
              >
                登录
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
              {'切换至' + (colorScheme === 'dark' ? '浅色' : '深色') + '主题'}
            </Menu.Item>
          </Menu.Dropdown>
        </Menu>
      </Group>
    </Header>
  )
}

export default AppHeader
