import { Button, Group, GroupProps, LoadingOverlay, Stack, Tabs } from '@mantine/core'
import {
  mdiAccountGroupOutline,
  mdiBullhornOutline,
  mdiFileDocumentCheckOutline,
  mdiFlagOutline,
  mdiKeyboardBackspace,
  mdiTextBoxOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import AdminPage from '@Components/admin/AdminPage'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'

export interface GameEditTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
  contentPos?: React.CSSProperties['justifyContent']
  isLoading?: boolean
  backUrl?: string
}

const WithGameEditTab: FC<GameEditTabProps> = ({
  children,
  isLoading,
  contentPos,
  head,
  backUrl,
  ...others
}) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id } = useParams()
  const { t } = useTranslation()

  const pages = [
    { icon: mdiTextBoxOutline, title: t('admin.tab.games.info'), path: 'info' },
    { icon: mdiBullhornOutline, title: t('admin.tab.games.notices'), path: 'notices' },
    { icon: mdiFlagOutline, title: t('admin.tab.games.challenges'), path: 'challenges' },
    { icon: mdiAccountGroupOutline, title: t('admin.tab.games.review'), path: 'review' },
    { icon: mdiFileDocumentCheckOutline, title: t('admin.tab.games.writeups'), path: 'writeups' },
  ]

  const getTab = (path: string) => pages.find((page) => path.includes(page.path))

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
    <AdminPage
      {...others}
      head={
        <>
          <Button
            w="9rem"
            styles={{ inner: { justifyContent: 'space-between' } }}
            leftSection={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate(backUrl ?? '/admin/games')}
          >
            {t('admin.button.back')}
          </Button>
          <Group wrap="nowrap" justify={contentPos ?? 'space-between'} w="calc(100% - 10rem)">
            {head}
          </Group>
        </>
      }
    >
      <Group wrap="nowrap" justify="space-between" align="flex-start" w="100%" pb="xl">
        <Tabs
          orientation="vertical"
          value={activeTab}
          onChange={(value) => value && navigate(`/admin/games/${id}/${value}`)}
          styles={{
            root: {
              width: '9rem',
            },
            list: {
              width: '9rem',
            },
          }}
        >
          <Tabs.List>
            {pages.map((page) => (
              <Tabs.Tab
                key={page.path}
                leftSection={<Icon path={page.icon} size={1} />}
                value={page.path}
              >
                {page.title}
              </Tabs.Tab>
            ))}
          </Tabs.List>
        </Tabs>
        <Stack w="calc(100% - 10rem)" pos="relative">
          <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />

          {children}
        </Stack>
      </Group>
    </AdminPage>
  )
}

export default WithGameEditTab
