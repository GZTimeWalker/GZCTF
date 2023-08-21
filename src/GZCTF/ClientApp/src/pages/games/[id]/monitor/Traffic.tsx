import dayjs from 'dayjs'
import { CSSProperties, FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Grid, Paper } from '@mantine/core'
import ScrollSelect from '@Components/ScrollSelect'
import { ChallengeItem, TeamItem, FileItem } from '@Components/TrafficItems'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import api, { FileRecord } from '@Api'

const SWROptions = {
  refreshInterval: 0,
  shouldRetryOnError: false,
  revalidateOnFocus: false,
}

const Traffic: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')

  const [challengeId, setChallengeId] = useState<number | null>(null)
  const [participationId, setParticipationId] = useState<number | null>(null)

  const { data: challengeTraffic } = api.game.useGameGetChallengesWithTrafficCapturing(
    gameId,
    SWROptions
  )
  const { data: teamTraffic } = api.game.useGameGetChallengeTraffic(
    challengeId ?? 0,
    SWROptions,
    !!challengeId
  )
  const { data: fileRecords } = api.game.useGameGetTeamTrafficAll(
    challengeId ?? 0,
    participationId ?? 0,
    SWROptions,
    !!challengeId && !!participationId
  )

  const onDownload = (item: FileRecord) => {
    if (!challengeId || !participationId || !item.fileName) return

    window.open(`/api/game/captures/${challengeId}/${participationId}/${item.fileName}`, '_blank')
  }

  const orderedFileRecords =
    fileRecords?.sort((a, b) => dayjs(b.updateTime).diff(dayjs(a.updateTime))) ?? []

  // make list longer for testing by duplicating 3 times
  const testFiles = orderedFileRecords.concat(orderedFileRecords).concat(orderedFileRecords)

  const innerStyle: CSSProperties = {
    borderRight: '1px solid gray',
  }

  return (
    <WithGameMonitorTab>
      <Paper shadow="md">
        <Grid gutter={0} h="calc(100vh - 110px)" grow>
          <Grid.Col span={3}>
            <ScrollSelect
              style={innerStyle}
              itemComponent={ChallengeItem}
              items={challengeTraffic}
              emptyPlaceholder="暂无题目"
              selectedId={challengeId}
              onSelectId={setChallengeId}
            />
          </Grid.Col>
          <Grid.Col span={3}>
            <ScrollSelect
              style={innerStyle}
              itemComponent={TeamItem}
              items={teamTraffic}
              emptyPlaceholder="暂无队伍"
              selectedId={participationId}
              onSelectId={setParticipationId}
            />
          </Grid.Col>
          <Grid.Col span={6}>
            <ScrollSelect
              itemComponent={FileItem}
              items={testFiles}
              emptyPlaceholder="暂无文件"
              customClick
              onSelectId={onDownload}
            />
          </Grid.Col>
        </Grid>
      </Paper>
    </WithGameMonitorTab>
  )
}

export default Traffic
