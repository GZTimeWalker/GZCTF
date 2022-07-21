import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { Group, GroupProps, ScrollArea, Stack, Tabs } from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiBullhornOutline,
  mdiFlagOutline,
  mdiAccountGroupOutline,
  mdiFileDocumentOutline,
} from '@mdi/js'
import Icon from '@mdi/react'
import LogoHeader from '../LogoHeader'

const pages = [
  { icon: mdiBullhornOutline, title: '通知管理', path: '/admin/notices' },
  { icon: mdiFlagOutline, title: '比赛管理', path: '/admin/games' },
  { icon: mdiAccountGroupOutline, title: '队伍管理', path: '/admin/teams' },
  { icon: mdiAccountCogOutline, title: '用户管理', path: '/admin/users' },
  { icon: mdiFileDocumentOutline, title: '系统日志', path: '/admin/logs' },
]

export interface AdminTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
}

const getTab = (path: string) => pages.findIndex((page) => path.startsWith(page.path))

const WithAdminTab: FC<AdminTabProps> = ({ head, headProps, children }) => {
  const navigate = useNavigate()
  const location = useLocation()

  const [activeTab, setActiveTab] = useState(
    getTab(location.pathname) < 0 ? 0 : getTab(location.pathname)
  )

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    navigate(tabKey)
  }

  useEffect(() => {
    const tab = pages.findIndex((page) => location.pathname.startsWith(page.path))

    if (tab >= 0) {
      setActiveTab(tab)
    }
  }, [location])

  return (
    <Stack spacing="xs">
      <Group position="apart">
        <LogoHeader />
        <Tabs grow tabPadding="md" position="apart" active={activeTab} onTabChange={onChange}>
          {pages.map((page) => (
            <Tabs.Tab
              label={page.title}
              icon={<Icon path={page.icon} size={1} />}
              tabKey={page.path}
            />
          ))}
        </Tabs>
      </Group>
      {head && (
        <Group position="apart" style={{ height: '40px', width: '100%' }} {...headProps}>
          {head}
        </Group>
      )}
      <ScrollArea
        offsetScrollbars
        style={{ height: head ? 'calc(100vh - 160px)' : 'calc(100vh - 120px)' }}
      >
        {children}
      </ScrollArea>
    </Stack>
  )
}

export default WithAdminTab
