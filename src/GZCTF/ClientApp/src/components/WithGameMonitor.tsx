import { Button, Group, LoadingOverlay, Stack, Tabs, useMantineTheme } from '@mantine/core'
import {
  mdiExclamationThick,
  mdiFileTableOutline,
  mdiFlag,
  mdiLightningBolt,
  mdiPackageVariant,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { downloadBlob } from '@Utils/ApiHelper'
import api, { Role } from '@Api'

interface WithGameMonitorProps extends React.PropsWithChildren {
  isLoading?: boolean
}

const WithGameMonitor: FC<WithGameMonitorProps> = ({ children, isLoading }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const navigate = useNavigate()
  const location = useLocation()
  const theme = useMantineTheme()

  const { t } = useTranslation()

  const pages = [
    { icon: mdiLightningBolt, title: t('game.tab.monitor.events'), path: 'events' },
    { icon: mdiFlag, title: t('game.tab.monitor.submissions'), path: 'submissions' },
    { icon: mdiExclamationThick, title: t('game.tab.monitor.cheatinfo'), path: 'cheatinfo' },
    { icon: mdiPackageVariant, title: t('game.tab.monitor.traffic'), path: 'traffic' },
  ]

  const getTab = (path: string) => pages.find((page) => path.endsWith(page.path))

  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.path ?? pages[0].path)
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.path ?? '')
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  const onDownloadScoreboardSheet = () =>
    downloadBlob(api.game.gameScoreboardSheet(numId, { format: 'blob' }), setDisabled, t)

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.Monitor}>
        <WithGameTab>
          <Group position="apart" align="flex-start">
            <Stack>
              <Button
                disabled={disabled}
                w="9rem"
                styles={{ inner: { justifyContent: 'space-between' } }}
                leftIcon={<Icon path={mdiFileTableOutline} size={1} />}
                onClick={onDownloadScoreboardSheet}
              >
                {t('game.button.download.scoreboard')}
              </Button>
              <Tabs
                orientation="vertical"
                value={activeTab}
                onTabChange={(value) => navigate(`/games/${id}/monitor/${value}`)}
                styles={{
                  root: {
                    width: '9rem',
                  },
                  tabsList: {
                    width: '9rem',
                  },
                }}
              >
                <Tabs.List>
                  {pages.map((page) => (
                    <Tabs.Tab
                      key={page.path}
                      icon={<Icon path={page.icon} size={1} />}
                      value={page.path}
                    >
                      {page.title}
                    </Tabs.Tab>
                  ))}
                </Tabs.List>
              </Tabs>
            </Stack>
            <Stack w="calc(100% - 10rem)" pos="relative">
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
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default WithGameMonitor
