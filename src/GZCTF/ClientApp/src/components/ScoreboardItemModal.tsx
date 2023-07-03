import dayjs from 'dayjs'
import { FC } from 'react'
import {
  Group,
  Text,
  Modal,
  ModalProps,
  ScrollArea,
  Stack,
  Table,
  Progress,
  Center,
  LoadingOverlay,
  Avatar,
  Title,
  Badge,
} from '@mantine/core'
import { BloodsTypes, BonusLabel } from '@Utils/Shared'
import { useTableStyles } from '@Utils/ThemeOverride'
import { ChallengeInfo, ScoreboardItem, SubmissionType } from '@Api'
import TeamRadarMap from './TeamRadarMap'

interface ScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  challenges?: Record<string, ChallengeInfo[]>
}

const ScoreboardItemModal: FC<ScoreboardItemModalProps> = (props) => {
  const { item, challenges, bloodBonusMap, ...modalProps } = props
  const { classes } = useTableStyles()

  const challengeIdMap =
    challenges &&
    Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => {
        map.set(challenge.id!, challenge)
      })
      return map
    }, new Map<number, ChallengeInfo>())

  const solved = (item?.solvedCount ?? 0) / (item?.challenges?.length ?? 1)

  const indicator =
    challenges &&
    Object.keys(challenges).map((tag) => ({
      name: tag,
      scoreSum: challenges[tag].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))

  const values = new Array(item?.challenges?.length ?? 0).fill(0)

  item?.challenges?.forEach((chal) => {
    if (indicator && challengeIdMap && chal) {
      const challenge = challengeIdMap.get(chal.id!)
      const index = challenge && indicator?.findIndex((ch) => ch.name === challenge.tag)
      if (chal?.score && challenge?.score && index !== undefined && index !== -1) {
        values[index] += challenge?.score / indicator[index].scoreSum
      }
    }
  })

  return (
    <Modal
      {...modalProps}
      title={
        <Group position="left" spacing="md" noWrap>
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md" color="brand">
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack spacing={0}>
            <Group spacing={4}>
              <Title order={4} lineClamp={1} weight="bold">
                {item?.name ?? 'Team'}
              </Title>
              {item?.organization && (
                <Badge size="sm" variant="outline">
                  {item.organization}
                </Badge>
              )}
            </Group>
            <Text size="sm" lineClamp={1}>
              {item?.bio || '这只队伍很懒，什么都没留下'}
            </Text>
          </Stack>
        </Group>
      }
    >
      <Stack align="center" spacing="xs">
        <Stack w="60%" miw="20rem">
          <Center h="14rem">
            <LoadingOverlay visible={!indicator || !values} />
            {item && indicator && values && (
              <TeamRadarMap indicator={indicator} value={values} name={item?.name ?? ''} />
            )}
          </Center>
          <Group grow ta="center">
            <Stack spacing={2}>
              <Text weight={700} size="sm" className={classes.mono}>
                {item?.rank}
              </Text>
              <Text size="xs">总排名</Text>
            </Stack>
            {item?.organization && (
              <Stack spacing={2}>
                <Text weight={700} size="sm" className={classes.mono}>
                  {item?.organizationRank}
                </Text>
                <Text size="xs">排名</Text>
              </Stack>
            )}
            <Stack spacing={2}>
              <Text weight={700} size="sm" className={classes.mono}>
                {item?.score}
              </Text>
              <Text size="xs">得分</Text>
            </Stack>
            <Stack spacing={2}>
              <Text weight={700} size="sm" className={classes.mono}>
                {item?.solvedCount}
              </Text>
              <Text size="xs">攻克数量</Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%">
            <Table className={classes.table}>
              <thead>
                <tr>
                  <th>用户</th>
                  <th>题目</th>
                  <th>类型</th>
                  <th>得分</th>
                  <th>时间</th>
                </tr>
              </thead>
              <tbody>
                {item?.challenges &&
                  challengeIdMap &&
                  item.challenges
                    .filter((c) => c.type !== SubmissionType.Unaccepted)
                    .sort((a, b) => dayjs(b.time).diff(dayjs(a.time)))
                    .map((chal) => {
                      const info = challengeIdMap.get(chal.id!)
                      return (
                        <tr key={chal.id}>
                          <td style={{ fontWeight: 500 }}>{chal.userName}</td>
                          <td>{info?.title}</td>
                          <td className={classes.mono}>{info?.tag}</td>
                          <td className={classes.mono}>
                            {chal.score}
                            {chal.score! > info?.score! &&
                              chal.type &&
                              BloodsTypes.includes(chal.type) && (
                                <Text span color="dimmed" className={classes.mono}>
                                  {` (${bloodBonusMap.get(chal.type)?.desrc})`}
                                </Text>
                              )}
                          </td>
                          <td className={classes.mono}>
                            {dayjs(chal.time).format('MM/DD HH:mm:ss')}
                          </td>
                        </tr>
                      )
                    })}
              </tbody>
            </Table>
          </ScrollArea>
        ) : (
          <Text py="1rem" weight={700}>
            Ouch! 这支队伍还没有解出题目呢……
          </Text>
        )}
      </Stack>
    </Modal>
  )
}

export default ScoreboardItemModal
