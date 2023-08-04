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
  Input,
} from '@mantine/core'
import TeamRadarMap from '@Components/TeamRadarMap'
import { BonusLabel } from '@Utils/Shared'
import { useTableStyles } from '@Utils/ThemeOverride'
import { ChallengeInfo, ScoreboardItem, SubmissionType } from '@Api'

interface MobileScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  challenges?: Record<string, ChallengeInfo[]>
}

const MobileScoreboardItemModal: FC<MobileScoreboardItemModalProps> = (props) => {
  const { item, challenges, ...modalProps } = props
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
              <Title order={4} lineClamp={1}>
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
            <Stack spacing={1}>
              <Text fw={700} size="sm" className={classes.mono}>
                {item?.rank}
              </Text>
              <Text size="xs">总排名</Text>
            </Stack>
            {item?.organization && (
              <Stack spacing={1}>
                <Text fw={700} size="sm" className={classes.mono}>
                  {item?.organizationRank}
                </Text>
                <Text size="xs">排名</Text>
              </Stack>
            )}
            <Stack spacing={1}>
              <Text fw={700} size="sm" className={classes.mono}>
                {item?.score}
              </Text>
              <Text size="xs">得分</Text>
            </Stack>
            <Stack spacing={1}>
              <Text fw={700} size="sm" className={classes.mono}>
                {item?.solvedCount}
              </Text>
              <Text size="xs">攻克数量</Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} size="sm" />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%">
            <Table
              className={classes.table}
              styles={{
                fontSize: '0.85rem',
              }}
            >
              <thead>
                <tr>
                  <th style={{ minWidth: '3rem' }}>题目</th>
                  <th style={{ minWidth: '3rem' }}>得分</th>
                  <th style={{ minWidth: '3rem' }}>时间</th>
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
                          <td>
                            <Input
                              variant="unstyled"
                              value={info?.title}
                              readOnly
                              size="sm"
                              sx={(theme) => ({
                                wrapper: {
                                  width: '100%',
                                },

                                input: {
                                  userSelect: 'none',
                                  fontSize: '0.85rem',
                                  lineHeight: '0.9rem',
                                  height: '0.9rem',
                                  fontWeight: 500,

                                  ...theme.fn.hover({
                                    cursor: 'pointer',
                                  }),
                                },
                              })}
                            />
                          </td>
                          <td className={classes.mono}>{chal.score}</td>
                          <td className={classes.mono}>{dayjs(chal.time).format('MM/DD HH:mm')}</td>
                        </tr>
                      )
                    })}
              </tbody>
            </Table>
          </ScrollArea>
        ) : (
          <Text py="1rem" fw={700}>
            Ouch! 这支队伍还没有解出题目呢……
          </Text>
        )}
      </Stack>
    </Modal>
  )
}

export default MobileScoreboardItemModal
