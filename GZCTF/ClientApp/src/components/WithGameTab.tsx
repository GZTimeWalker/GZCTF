import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Card, Stack, Title, Text, LoadingOverlay, useMantineTheme } from '@mantine/core'
import { useInterval } from '@mantine/hooks'
import { mdiFlagOutline, mdiGauge, mdiMedalOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { usePageTitle } from '@Utils/PageTitle'
import api, { GameDetailModel, ParticipationStatus, Role } from '@Api'
import CustomProgress from './CustomProgress'
import IconTabs from './IconTabs'
import { RoleMap } from './WithRole'

const pages = [
  {
    icon: mdiGauge,
    title: '比赛监控',
    path: 'monitor',
    link: 'monitor/events',
    color: 'green',
    requireJoin: false,
    requireRole: Role.Monitor,
  },
  {
    icon: mdiFlagOutline,
    title: '比赛题目',
    path: 'challenges',
    link: 'challenges',
    color: 'blue',
    requireJoin: true,
    requireRole: Role.User,
  },
  {
    icon: mdiMedalOutline,
    title: '积分总榜',
    path: 'scoreboard',
    link: 'scoreboard',
    color: 'yellow',
    requireJoin: false,
    requireRole: Role.User,
  },
]

const getTab = (path: string) => pages.findIndex((page) => path.includes(page.path))
dayjs.extend(duration)

interface WithGameTabProps extends React.PropsWithChildren {
  game?: GameDetailModel
  isLoading?: boolean
  status?: ParticipationStatus
}

const WithGameTab: FC<WithGameTabProps> = ({ game, isLoading, status, children }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const location = useLocation()
  const navigate = useNavigate()

  const theme = useMantineTheme()
  const tabIndex = getTab(location.pathname)
  const [activeTab, setActiveTab] = useState(tabIndex < 0 ? 0 : tabIndex)

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    navigate(`/games/${numId}/${tabKey}`)
  }

  const { data: user } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const start = dayjs(game?.start ?? new Date())
  const end = dayjs(game?.end ?? new Date())
  const total = end.diff(start)

  const [now, setNow] = useState(dayjs())
  const interval = useInterval(() => setNow(dayjs()), 1000)
  const current = now.diff(start)

  const progress = (current / total) * 100
  const countdown = dayjs.duration(end.diff(now))

  usePageTitle(game?.title)

  useEffect(() => {
    if (game && dayjs() < dayjs(game.end)) {
      interval.start()
      return interval.stop
    }
  }, [game])

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab >= 0) {
      setActiveTab(tab)
    } else {
      navigate(pages[0].path)
    }
  }, [location])

  return (
    <Stack style={{ position: 'relative' }}>
      <LoadingOverlay
        visible={isLoading ?? false}
        overlayOpacity={1}
        overlayColor={theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]}
      />
      <IconTabs
        active={activeTab}
        onTabChange={onChange}
        tabs={pages
          .filter((p) => RoleMap.get(user?.role ?? Role.User)! >= RoleMap.get(p.requireRole)!)
          .filter((p) => !p.requireJoin || status === ParticipationStatus.Accepted)
          .map((p) => ({
            tabKey: p.link,
            label: p.title,
            icon: <Icon path={p.icon} size={1} />,
            color: p.color,
          }))}
        left={
          game && (
            <>
              <Title>{game?.title}</Title>
              <Card
                style={{
                  width: '8rem',
                  textAlign: 'center',
                  paddingTop: '4px',
                  overflow: 'visible',
                }}
              >
                <Text style={{ fontWeight: 700 }}>
                  {countdown.asSeconds() > 0
                    ? `${countdown.days() * 24 + countdown.hours()} : ${countdown.format(
                        'mm : ss'
                      )}`
                    : '比赛已结束'}
                </Text>
                <Card.Section style={{ marginTop: 4 }}>
                  <CustomProgress percentage={progress} py={0} />
                </Card.Section>
              </Card>
            </>
          )
        }
      />
      {children}
    </Stack>
  )
}

export default WithGameTab
