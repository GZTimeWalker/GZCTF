import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Avatar, Group, NavLink, Paper, ScrollArea } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiFileOutline, mdiClose, mdiPuzzle } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { ChallengeTypeLabelMap, ChallengeTagLabelMap, HunamizeSize } from '@Utils/Shared'
import api, { ChallengeTrafficModel, FileRecord, TeamTrafficModel } from '@Api'

const Traffic: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')
  const [challengeId, setChallengeId] = useState<number | null>(null)
  const [participationId, setParticipationId] = useState<number | null>(null)

  const [challengeTraffic, setChallengeTraffic] = useState<
    ChallengeTrafficModel[] | null | undefined
  >(null)
  const [teamTraffic, setTeamTraffic] = useState<TeamTrafficModel[] | null | undefined>(null)
  const [fileRecords, setFileRecords] = useState<FileRecord[] | null | undefined>(null)

  useEffect(() => {
    setChallengeId(null)
    setChallengeTraffic(undefined)
    api.game
      .gameGetChallengesWithTrafficCapturing(gameId)
      .then((data) => {
        setChallengeTraffic(data.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '获取题目列表失败',
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [gameId])

  useEffect(() => {
    setParticipationId(null)
    if (challengeId) {
      setTeamTraffic(undefined)
      api.game
        .gameGetChallengeTraffic(challengeId)
        .then((data) => {
          setTeamTraffic(data.data)
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '获取队伍列表失败',
            message: err.response.data.title,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
    } else {
      setTeamTraffic(null)
    }
  }, [challengeId])

  useEffect(() => {
    if (challengeId && participationId) {
      setFileRecords(undefined)
      api.game
        .gameGetTeamTrafficAll(challengeId, participationId)
        .then((data) => {
          setFileRecords(data.data)
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '获取流量包列表失败',
            message: err.response.data.title,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
    } else {
      setFileRecords(null)
    }
  }, [challengeId, participationId])

  const onDownload = (filename: string) => {
    if (challengeId && participationId && filename) {
      window.open(`/api/game/captures/${challengeId}/${participationId}/${filename}`)
    } else {
      showNotification({
        color: 'red',
        title: '客户端发生错误',
        message: '请尝试刷新页面',
        icon: <Icon path={mdiClose} size={1} />,
      })
    }
  }

  return (
    <WithGameMonitorTab>
      <Paper shadow="md">
        <Group grow spacing={0} h="calc(100vh - 100px)">
          <Group grow spacing={0} h="100%">
            <ScrollArea
              h="100%"
              style={{
                borderRight: '1px solid gray',
              }}
            >
              {challengeTraffic === undefined
                ? 'Loading'
                : challengeTraffic === null || challengeTraffic.length === 0
                ? 'No Data'
                : challengeTraffic.map((value) => (
                    <NavLink
                      icon={
                        <Icon
                          path={ChallengeTagLabelMap.get(value.tag!)?.icon ?? mdiPuzzle}
                          size={1.5}
                        />
                      }
                      label={value.title}
                      description={ChallengeTypeLabelMap.get(value.type!)?.label}
                      rightSection={value.count}
                      key={value.id}
                      active={value.id === challengeId}
                      onClick={() => setChallengeId(value.id!)}
                      variant="filled"
                    />
                  ))}
            </ScrollArea>
            <ScrollArea
              h="100%"
              style={{
                borderRight: '1px solid gray',
              }}
            >
              {teamTraffic === undefined
                ? 'Loading'
                : teamTraffic === null || teamTraffic.length === 0
                ? 'No Data'
                : teamTraffic.map((value) => (
                    <NavLink
                      icon={
                        <Avatar alt="avatar" src={value.avatar}>
                          {value.name?.slice(0, 1) ?? 'T'}
                        </Avatar>
                      }
                      label={value.name}
                      description={value.organization}
                      rightSection={value.count}
                      key={value.participationId}
                      active={value.participationId === participationId}
                      onClick={() => setParticipationId(value.participationId!)}
                      variant="filled"
                    />
                  ))}
            </ScrollArea>
          </Group>
          <ScrollArea h="100%">
            {fileRecords === undefined
              ? 'Loading'
              : fileRecords === null || fileRecords.length === 0
              ? 'No Data'
              : fileRecords.map((value) => (
                  <NavLink
                    icon={<Icon path={mdiFileOutline} size={1.5} />}
                    label={value.fileName}
                    description={dayjs(value.updateTime).format('YYYY/MM/DD HH:mm:ss')}
                    rightSection={HunamizeSize(value.size!)}
                    key={value.fileName}
                    onClick={() => onDownload(value.fileName!)}
                  />
                ))}
          </ScrollArea>
        </Group>
      </Paper>
    </WithGameMonitorTab>
  )
}

export default Traffic
