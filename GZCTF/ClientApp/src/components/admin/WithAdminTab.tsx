import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Group, GroupProps, LoadingOverlay, Stack, useMantineTheme } from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiFlagOutline,
  mdiAccountGroupOutline,
  mdiFileDocumentOutline,
  mdiSitemapOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { usePageTitle } from '@Utils/usePageTitle'
import IconTabs from '../IconTabs'

const pages = [
  { icon: mdiFlagOutline, title: '比赛管理', path: 'games', color: 'yellow' },
  { icon: mdiAccountGroupOutline, title: '队伍管理', path: 'teams', color: 'green' },
  { icon: mdiAccountCogOutline, title: '用户管理', path: 'users', color: 'cyan' },
  { icon: mdiFileDocumentOutline, title: '系统日志', path: 'logs', color: 'red' },
  { icon: mdiSitemapOutline, title: '全局设置', path: 'configs', color: 'orange' },
]

export interface AdminTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  isLoading?: boolean
  headProps?: GroupProps
}

const getTab = (path: string) => pages.findIndex((page) => path.startsWith(`/admin/${page.path}`))

const WithAdminTab: FC<AdminTabProps> = ({ head, headProps, isLoading, children }) => {
  const navigate = useNavigate()
  const location = useLocation()

  const theme = useMantineTheme()
  const tabIndex = getTab(location.pathname)
  const [activeTab, setActiveTab] = useState(tabIndex < 0 ? 0 : tabIndex)

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    navigate(`/admin/${tabKey}`)
  }

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab >= 0) {
      setActiveTab(tab)
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  usePageTitle(pages[tabIndex].title)

  return (
    <Stack spacing="xs" align="center">
      <IconTabs
        withIcon
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
      <LoadingOverlay
        visible={isLoading ?? false}
        overlayOpacity={1}
        overlayColor={theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]}
      />
      {children}
    </Stack>
  )
}

export default WithAdminTab
