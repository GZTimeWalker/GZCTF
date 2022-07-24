import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  Group,
  GroupProps,
  LoadingOverlay,
  Stack,
  Tabs,
  useMantineTheme,
} from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiBullhornOutline,
  mdiAccountGroupOutline,
  mdiPencilOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import AdminPage from './AdminPage'

const pages = [
  { icon: mdiPencilOutline, title: '信息编辑', path: 'info' },
  { icon: mdiBullhornOutline, title: '比赛通知', path: 'notices' },
  { icon: mdiAccountGroupOutline, title: '题目编辑', path: 'challenges' },
  { icon: mdiAccountCogOutline, title: '队伍审核', path: 'review' },
]

interface GameTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
  isLoading?: boolean
}

const getTab = (path: string) =>
  pages.findIndex((page) => path.startsWith('/admin/games/') && path.includes(page.path))

const WithGameTab: FC<GameTabProps> = ({ children, isLoading, ...others }) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id } = useParams()
  const theme = useMantineTheme()
  const tabIndex = getTab(location.pathname)
  const [activeTab, setActiveTab] = useState(tabIndex < 0 ? 0 : tabIndex)

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    navigate(tabKey)
  }

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab >= 0) {
      setActiveTab(tab)
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  return (
    <AdminPage {...others}>
      <Group position="apart" align="flex-start">
        <Tabs
          orientation="vertical"
          active={activeTab}
          onTabChange={onChange}
          styles={{
            root: {
              width: '8rem',
            },
          }}
        >
          {pages.map((page) => (
            <Tabs.Tab
              label={page.title}
              icon={<Icon path={page.icon} size={1} />}
              tabKey={`/admin/games/${id}/${page.path}`}
            />
          ))}
        </Tabs>
        <Stack style={{ width: 'calc(100% - 9rem)', position: 'relative' }}>
          <LoadingOverlay
            visible={isLoading ?? false}
            overlayOpacity={1}
            overlayColor={
              theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.gray[0]
            }
          />
          {children}
        </Stack>
      </Group>
    </AdminPage>
  )
}

export default WithGameTab
