import dayjs from 'dayjs'
import { CSSProperties, FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Center, Grid, NavLink, Paper, ScrollArea, ScrollAreaProps, Stack } from '@mantine/core'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { HunamizeSize } from '@Utils/Shared'
import api, { ChallengeTrafficModel, FileRecord, TeamTrafficModel } from '@Api'

interface SelectableItemProps {
  onClick: () => void
  active: boolean
}

interface ScrollSelectProps extends ScrollAreaProps {
  itemComponent: React.FC<any>
  emptyPlaceholder?: React.ReactNode
  items?: any[]
  customClick?: boolean
  selectedId?: number | null
  onSelectId: (item: any | null) => void
}

const ScrollSelect: FC<ScrollSelectProps> = (props) => {
  const {
    itemComponent: ItemComponent,
    emptyPlaceholder,
    items,
    selectedId,
    onSelectId,
    customClick,
    ...ScrollAreaProps
  } = props

  return (
    <ScrollArea h="100%" {...ScrollAreaProps}>
      {!items || items.length === 0 ? (
        <Center h="100%">{emptyPlaceholder}</Center>
      ) : (
        <Stack spacing="xs" w="100%">
          {customClick
            ? items.map((item) => (
                <ItemComponent
                  key={item.id}
                  onClick={() => onSelectId(item)}
                  active={false}
                  {...item}
                />
              ))
            : items.map((item) => (
                <ItemComponent
                  key={item.id}
                  onClick={() => onSelectId(item.id)}
                  active={selectedId === item.id}
                  {...item}
                />
              ))}
        </Stack>
      )}
    </ScrollArea>
  )
}

const ChallengeItem: FC<ChallengeTrafficModel & SelectableItemProps> = (itemProps) => {
  const { onClick, active, ...props } = itemProps

  return (
    <NavLink
      label={props.title}
      description={props.tag}
      rightSection={props.count}
      onClick={onClick}
      active={active}
      variant="filled"
    />
  )
}

const TeamItem: FC<TeamTrafficModel & SelectableItemProps> = (itemProps) => {
  const { onClick, active, ...props } = itemProps

  return (
    <NavLink
      label={props.name}
      description={props.organization}
      rightSection={props.count}
      onClick={onClick}
      active={active}
      variant="filled"
    />
  )
}

const FileItem: FC<FileRecord & SelectableItemProps> = (itemProps) => {
  const { onClick, active, ...props } = itemProps

  return (
    <NavLink
      label={props.fileName}
      description={dayjs(props.updateTime).format('YYYY/MM/DD HH:mm:ss')}
      rightSection={HunamizeSize(props.size!)}
      onClick={onClick}
      active={active}
      variant="filled"
    />
  )
}

const Traffic: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')

  const [challengeId, setChallengeId] = useState<number | null>(null)
  const [participationId, setParticipationId] = useState<number | null>(null)

  const { data: challengeTraffic } = api.game.useGameGetChallengesWithTrafficCapturing(gameId)
  const { data: teamTraffic } = api.game.useGameGetChallengeTraffic(
    challengeId ?? 0,
    {},
    !!challengeId
  )
  const { data: fileRecords } = api.game.useGameGetTeamTrafficAll(
    challengeId ?? 0,
    participationId ?? 0,
    {},
    !!challengeId && !!participationId
  )

  const onDownload = (item: FileRecord) => {
    if (!challengeId || !participationId || !item.fileName) return

    window.open(`/api/game/captures/${challengeId}/${participationId}/${item.fileName}`, '_blank')
  }

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
              items={fileRecords}
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
