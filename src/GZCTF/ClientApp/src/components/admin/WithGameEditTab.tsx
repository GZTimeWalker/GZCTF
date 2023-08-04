import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Group, GroupProps, LoadingOverlay, Stack, Tabs, useMantineTheme } from '@mantine/core'
import {
  mdiAccountGroupOutline,
  mdiBullhornOutline,
  mdiFileDocumentCheckOutline,
  mdiFlagOutline,
  mdiTextBoxOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import AdminPage from '@Components/admin/AdminPage'

const pages = [
  { icon: mdiTextBoxOutline, title: '信息编辑', path: 'info' },
  { icon: mdiBullhornOutline, title: '比赛通知', path: 'notices' },
  { icon: mdiFlagOutline, title: '题目编辑', path: 'challenges' },
  { icon: mdiAccountGroupOutline, title: '队伍审核', path: 'review' },
  { icon: mdiFileDocumentCheckOutline, title: 'Writeups', path: 'writeups' },
]

interface GameEditTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
  isLoading?: boolean
}

const getTab = (path: string) => pages.find((page) => path.includes(page.path))

const WithGameEditTab: FC<GameEditTabProps> = ({ children, isLoading, ...others }) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id } = useParams()
  const theme = useMantineTheme()
  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.path ?? pages[0].path)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.path ?? '')
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  return (
    <AdminPage {...others}>
      <Group noWrap position="apart" align="flex-start" w="100%">
        <Tabs
          orientation="vertical"
          value={activeTab}
          onTabChange={(value) => navigate(`/admin/games/${id}/${value}`)}
          styles={{
            root: {
              width: '8rem',
            },
          }}
        >
          <Tabs.List>
            {pages.map((page) => (
              <Tabs.Tab key={page.path} icon={<Icon path={page.icon} size={1} />} value={page.path}>
                {page.title}
              </Tabs.Tab>
            ))}
          </Tabs.List>
        </Tabs>
        <Stack w="calc(100% - 9rem)" pos="relative">
          <LoadingOverlay
            visible={isLoading ?? false}
            overlayOpacity={1}
            overlayColor={
              theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]
            }
          />
          {children}
        </Stack>
      </Group>
    </AdminPage>
  )
}

export default WithGameEditTab
