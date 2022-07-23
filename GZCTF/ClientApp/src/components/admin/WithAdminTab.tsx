import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Group, GroupProps, ScrollArea, Stack } from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiBullhornOutline,
  mdiFlagOutline,
  mdiAccountGroupOutline,
  mdiFileDocumentOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import IconTabs from '../IconTabs'

const pages = [
  { icon: mdiBullhornOutline, title: '通知管理', path: '/admin/notices', color: 'blue' },
  { icon: mdiFlagOutline, title: '比赛管理', path: '/admin/games', color: 'yellow' },
  { icon: mdiAccountGroupOutline, title: '队伍管理', path: '/admin/teams', color: 'green' },
  { icon: mdiAccountCogOutline, title: '用户管理', path: '/admin/users', color: 'cyan' },
  { icon: mdiFileDocumentOutline, title: '系统日志', path: '/admin/logs', color: 'red' },
]

export interface AdminTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
}

const getTab = (path: string) => pages.findIndex((page) => path.startsWith(page.path))

const WithAdminTab: FC<AdminTabProps> = ({ head, headProps, children }) => {
  const navigate = useNavigate()
  const location = useLocation()

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
    <Stack spacing="xs">
      <IconTabs
        active={activeTab}
        onTabChange={onChange}
        tabs={pages.map((p) => ({
          tabKey: p.path,
          label: p.title,
          icon: <Icon path={p.icon} size={1} />,
          color: p.color,
        }))}
      />
      {head && (
        <Group position="apart" style={{ height: '40px', width: '100%' }} {...headProps}>
          {head}
        </Group>
      )}
      <ScrollArea style={{ height: head ? 'calc(100vh - 160px)' : 'calc(100vh - 120px)' }}>
        {children}
      </ScrollArea>
    </Stack>
  )
}

export default WithAdminTab
