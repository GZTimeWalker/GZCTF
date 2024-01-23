import { Group, GroupProps, LoadingOverlay, Stack, useMantineTheme } from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiAccountGroupOutline,
  mdiFileDocumentOutline,
  mdiFlagOutline,
  mdiPackageVariantClosed,
  mdiSitemapOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import IconTabs from '@Components/IconTabs'
import { useTranslation } from '@Utils/I18n'
import { usePageTitle } from '@Utils/usePageTitle'

const pages = [
  { icon: mdiFlagOutline, title: t('比赛管理'), path: 'games', color: 'yellow' },
  { icon: mdiAccountGroupOutline, title: t('队伍管理'), path: 'teams', color: 'green' },
  { icon: mdiAccountCogOutline, title: t('用户管理'), path: 'users', color: 'cyan' },
  { icon: mdiPackageVariantClosed, title: t('容器管理'), path: 'instances', color: 'blue' },
  { icon: mdiFileDocumentOutline, title: t('系统日志'), path: 'logs', color: 'red' },
  { icon: mdiSitemapOutline, title: t('全局设置'), path: 'configs', color: 'orange' },
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

  const { t } = useTranslation()

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
    <Stack spacing="xs" align="center" pt="md">
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
        <Group noWrap position="apart" h="40px" w="100%" {...headProps}>
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
