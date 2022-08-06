import { GameDetailModel, ParticipationStatus } from '@Api/Api'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Card, Stack, Title, Text, Progress, LoadingOverlay, useMantineTheme } from '@mantine/core'
import { useInterval } from '@mantine/hooks'
import { mdiFlagOutline, mdiMedalOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import IconTabs from './IconTabs'

const pages = [
  { icon: mdiFlagOutline, title: '比赛题目', path: 'challenges', color: 'blue', requireJoin: true },
  {
    icon: mdiMedalOutline,
    title: '积分总榜',
    path: 'scoreboard',
    color: 'yellow',
    requireJoin: false,
  },
]

const getTab = (path: string) => pages.findIndex((page) => path.endsWith(page.path))
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

  const start = dayjs(game?.start ?? new Date())
  const end = dayjs(game?.end ?? new Date())
  const total = end.diff(start)

  const [now, setNow] = useState(dayjs())
  const interval = useInterval(() => setNow(dayjs()), 1000)
  const current = now.diff(start)

  const progress = (current / total) * 100
  const countdown = dayjs.duration(end.diff(now))

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
          .filter((p) => !p.requireJoin || status === ParticipationStatus.Accepted)
          .map((p) => ({
            tabKey: p.path,
            label: p.title,
            icon: <Icon path={p.icon} size={1} />,
            color: p.color,
          }))}
        left={
          game && (
            <>
              <Title>{game?.title}</Title>
              <Card style={{ width: '8rem', textAlign: 'center', paddingTop: '4px' }}>
                <Text style={{ fontWeight: 700 }}>
                  {countdown.asSeconds() > 0
                    ? `${countdown.days() * 24 + countdown.hours()} : ${countdown.format(
                        'mm : ss'
                      )}`
                    : '比赛已结束'}
                </Text>
                <Card.Section style={{ marginTop: '2px' }}>
                  <Progress radius="xs" size="sm" animate={progress < 100} value={progress} />
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
