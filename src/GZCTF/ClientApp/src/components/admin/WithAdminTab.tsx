import {
  Group,
  GroupProps,
  LoadingOverlay,
  Stack,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
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
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router-dom'
import IconTabs from '@Components/IconTabs'
import { usePageTitle } from '@Utils/usePageTitle'

export interface AdminTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  isLoading?: boolean
  headProps?: GroupProps
}

const WithAdminTab: FC<AdminTabProps> = ({ head, headProps, isLoading, children }) => {
  const navigate = useNavigate()
  const location = useLocation()

  const theme = useMantineTheme()
  const { t } = useTranslation()

  const pages = [
    { icon: mdiFlagOutline, title: t('admin.tab.games.index'), path: 'games', color: 'yellow' },
    { icon: mdiAccountGroupOutline, title: t('admin.tab.teams'), path: 'teams', color: 'green' },
    { icon: mdiAccountCogOutline, title: t('admin.tab.users'), path: 'users', color: 'cyan' },
    {
      icon: mdiPackageVariantClosed,
      title: t('admin.tab.instances'),
      path: 'instances',
      color: 'blue',
    },
    { icon: mdiFileDocumentOutline, title: t('admin.tab.logs'), path: 'logs', color: 'red' },
    { icon: mdiSitemapOutline, title: t('admin.tab.settings'), path: 'settings', color: 'orange' },
  ]
  const getTab = (path: string) => pages.findIndex((page) => path.startsWith(`/admin/${page.path}`))
  const tabIndex = getTab(location.pathname)
  const { colorScheme } = useMantineColorScheme()
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
    <Stack gap="xs" align="center" pt="md">
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
        <Group wrap="nowrap" justify="space-between" h="40px" w="100%" {...headProps}>
          {head}
        </Group>
      )}
      <LoadingOverlay
        visible={isLoading ?? false}
        opacity={1}
        c={colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.light[2]}
      />
      {children}
    </Stack>
  )
}

export default WithAdminTab
