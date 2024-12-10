import {
  alpha,
  Avatar,
  Box,
  Center,
  Grid,
  Group,
  Input,
  Pagination,
  Paper,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  Tooltip,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { mdiAccountGroup, mdiMagnify } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import dayjs from 'dayjs'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ScoreboardItemModal } from '@Components/ScoreboardItemModal'
import { useLanguage } from '@Utils/I18n'
import {
  BloodBonus,
  BloodsTypes,
  useChallengeCategoryLabelMap,
  SubmissionTypeIconMap,
  useBonusLabels,
  PartialIconProps,
} from '@Utils/Shared'
import { useGameScoreboard } from '@Hooks/useGame'
import { ChallengeInfo, ChallengeCategory, ScoreboardItem, SubmissionType } from '@Api'
import misc from '@Styles/Misc.module.css'
import classes from '@Styles/ScoreboardTable.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

const Widths = [60, 55, 170, 55, 70, 60]
const Lefts = Widths.reduce(
  (acc, cur) => {
    acc.push(acc[acc.length - 1] + cur)
    return acc
  },
  [0]
)

const TableHeader = (table: Record<string, ChallengeInfo[]>) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()

  const hiddenCol = [...Array(5).keys()].map((i) => (
    <Table.Th
      key={i}
      className={classes.left}
      style={{
        left: Lefts[i],
        width: Widths[i],
        minWidth: Widths[i],
        maxWidth: Widths[i],
      }}
    >
      &nbsp;
    </Table.Th>
  ))

  return (
    <Table.Thead className={classes.thead}>
      <Table.Tr className={misc.noBorder}>
        {hiddenCol}
        {Object.keys(table).map((key) => {
          const cate = challengeCategoryLabelMap.get(key as ChallengeCategory)!
          return (
            <Table.Th
              key={key}
              colSpan={table[key].length}
              h="3rem"
              style={{
                backgroundColor: alpha(
                  theme.colors[cate.color][colorScheme === 'dark' ? 8 : 6],
                  colorScheme === 'dark' ? 0.15 : 0.2
                ),
              }}
            >
              <Group gap={4} wrap="nowrap" justify="center" w="100%">
                <Icon path={cate.icon} size={1} color={theme.colors[cate.color][colorScheme === 'dark' ? 8 : 6]} />
                <Text c={cate.color} className={classes.text} ff="text" fz="sm">
                  {key}
                </Text>
              </Group>
            </Table.Th>
          )
        })}
      </Table.Tr>
      {/* Challenge Name */}
      <Table.Tr>
        {hiddenCol}
        {Object.keys(table).map((key) => table[key].map((item) => <Table.Th key={item.id}>{item.title}</Table.Th>))}
      </Table.Tr>
      {/* Headers & Score */}
      <Table.Tr>
        {[
          t('game.label.score_table.rank_total'),
          t('game.label.score_table.rank_division'),
          t('common.label.team'),
          t('game.label.score_table.solved_count'),
          t('game.label.score_table.score_total'),
        ].map((header, idx) => (
          <Table.Th key={idx} className={cx(classes.left, classes.header)} style={{ left: Lefts[idx] }}>
            {header}
          </Table.Th>
        ))}
        {Object.keys(table).map((key) =>
          table[key].map((item) => (
            <Table.Th key={item.id} className={classes.mono}>
              {item.score}
            </Table.Th>
          ))
        )}
      </Table.Tr>
    </Table.Thead>
  )
}

const TableRow: FC<{
  item: ScoreboardItem
  allRank: boolean
  tableRank: number
  onOpenDetail: () => void
  iconMap: Map<SubmissionType, PartialIconProps | undefined>
  challenges?: Record<string, ChallengeInfo[]>
}> = ({ item, challenges, onOpenDetail, iconMap, tableRank, allRank }) => {
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const solved = item.solvedChallenges
  const theme = useMantineTheme()
  const { locale } = useLanguage()

  return (
    <Table.Tr>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[0] }}>
        {item.rank}
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[1] }}>
        {allRank ? item.rank : (item.divisionRank ?? tableRank)}
      </Table.Td>
      <Table.Td className={classes.left} style={{ left: Lefts[2] }}>
        <Group
          justify="left"
          gap={5}
          wrap="nowrap"
          onClick={onOpenDetail}
          maw={Widths[2] - 10}
          className={classes.pointer}
        >
          <Avatar alt="avatar" src={item.avatar} radius="xl" size={30} color={theme.primaryColor}>
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0} h="2.5rem" justify="center" w={Widths[2] - 45}>
            <Input
              variant="unstyled"
              value={item.name}
              readOnly
              size="sm"
              __vars={{
                '--input-height': 'var(--mantine-line-height-sm)',
              }}
              classNames={{
                wrapper: cx(classes.pointer, classes.wapper),
                input: cx(classes.pointer, classes.input),
              }}
            />
            {!!item.division && (
              <Text size="xs" c="dimmed" ta="start" truncate className={classes.text}>
                {item.division}
              </Text>
            )}
          </Stack>
        </Group>
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[3] }}>
        {solved?.length}
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[4] }}>
        {solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0)}
      </Table.Td>
      {challenges &&
        Object.keys(challenges).map((key) =>
          challenges[key].map((item) => {
            const chal = solved?.find((c) => c.id === item.id)
            const icon = iconMap.get(chal?.type ?? SubmissionType.Unaccepted)

            if (!icon) return <Table.Td key={item.id} className={classes.mono} />

            const cate = challengeCategoryLabelMap.get(item.category as ChallengeCategory)!

            return (
              <Table.Td key={item.id} className={classes.mono}>
                <Tooltip
                  classNames={tooltipClasses}
                  transitionProps={{ transition: 'pop' }}
                  label={
                    <Stack align="flex-start" gap={0} maw="20rem">
                      <Text lineClamp={3} fz="xs" className={classes.text}>
                        {item.title}
                      </Text>
                      <Text c={cate.color} fz="xs" className={classes.text}>
                        + {chal?.score} pts
                      </Text>
                      <Text c="dimmed" fz="xs" className={classes.text}>
                        # {dayjs(chal?.time).locale(locale).format('L LTS')}
                      </Text>
                    </Stack>
                  }
                >
                  <Center>
                    <Icon {...icon} />
                  </Center>
                </Tooltip>
              </Table.Td>
            )
          })
        )}
    </Table.Tr>
  )
}

const ITEM_COUNT_PER_PAGE = 30

export interface ScoreboardProps {
  division: string | null
  setDivision: (div: string | null) => void
}

export const ScoreboardTable: FC<ScoreboardProps> = ({ division, setDivision }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { iconMap } = SubmissionTypeIconMap(1)
  const [activePage, setPage] = useState(1)
  const [bloodBonus, setBloodBonus] = useState(BloodBonus.default)

  const [keyword, setKeyword] = useState('')
  const [debouncedKeyword] = useDebouncedValue(keyword, 400)

  const [filteredList, setFilteredList] = useState<ScoreboardItem[]>([])

  const { scoreboard } = useGameScoreboard(numId)

  useEffect(() => {
    setPage(1)
    setDivision('all')
    setKeyword('')
  }, [id])

  useEffect(() => {
    if (!scoreboard?.items) return

    if (!!debouncedKeyword && debouncedKeyword.length > 0) {
      setFilteredList(scoreboard.items.filter((s) => s.name?.toLowerCase().includes(debouncedKeyword.toLowerCase())))
      return
    }

    if (division !== 'all') {
      setFilteredList(scoreboard.items.filter((s) => s.division === division))
      return
    }

    setFilteredList(scoreboard.items)
  }, [scoreboard, debouncedKeyword, division])

  const base = (activePage - 1) * ITEM_COUNT_PER_PAGE
  const currentItems = filteredList?.slice(base, base + ITEM_COUNT_PER_PAGE)

  const [currentItem, setCurrentItem] = useState<ScoreboardItem | null>(null)
  const [itemDetailOpened, setItemDetailOpened] = useState(false)

  const { t } = useTranslation()

  useEffect(() => {
    if (scoreboard) {
      setBloodBonus(new BloodBonus(scoreboard.bloodBonus))
    }
  }, [scoreboard])

  const bloodData = useBonusLabels(bloodBonus)
  const multiTimeline = scoreboard?.timeLines && Object.keys(scoreboard.timeLines).length > 1

  return (
    <Paper shadow="md" p="md">
      <Stack gap="xs">
        <Grid>
          <Grid.Col span={3}>
            <Select
              defaultValue="all"
              data={[
                { value: 'all', label: t('game.label.score_table.all_teams') },
                ...Object.keys(scoreboard?.timeLines ?? {})
                  .filter((k) => k !== 'all')
                  .map((o) => ({
                    value: o,
                    label: o === 'all' ? t('game.label.score_table.rank_total') : o,
                  })),
              ]}
              value={division}
              readOnly={!multiTimeline}
              onChange={(div) => {
                setDivision(div)
                setPage(1)
              }}
              leftSection={<Icon path={mdiAccountGroup} size={1} />}
            />
          </Grid.Col>
          <Grid.Col span={6} />
          <Grid.Col span={3}>
            <TextInput
              placeholder={t('game.placeholder.search_team')}
              value={keyword}
              onChange={(e) => setKeyword(e.currentTarget.value)}
              leftSection={<Icon path={mdiMagnify} size={1} />}
            />
          </Grid.Col>
        </Grid>
        <Box pos="relative" mih="calc(100vh - 14rem)">
          <Table.ScrollContainer
            minWidth="100%"
            classNames={{
              scrollContainer: misc.noScrollBars,
            }}
          >
            <Table className={classes.table}>
              <TableHeader {...scoreboard?.challenges} />
              <Table.Tbody>
                {scoreboard &&
                  currentItems?.map((item, idx) => (
                    <TableRow
                      key={base + idx}
                      allRank={division === 'all'}
                      tableRank={base + idx + 1}
                      item={item}
                      onOpenDetail={() => {
                        setCurrentItem(item)
                        setItemDetailOpened(true)
                      }}
                      challenges={scoreboard.challenges}
                      iconMap={iconMap}
                    />
                  ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
          <Box className={classes.legend}>
            <Stack gap="xs">
              <Tooltip.Group>
                <Group gap="lg">
                  {BloodsTypes.map((type, idx) => (
                    <Tooltip key={idx} label={bloodData.get(type)?.name} transitionProps={{ transition: 'pop' }}>
                      <Group justify="left" gap={2}>
                        <Icon {...iconMap.get(type)!} />
                        <Text>{bloodData.get(type)?.descr}</Text>
                      </Group>
                    </Tooltip>
                  ))}
                </Group>
              </Tooltip.Group>
              <Text size="sm" c="dimmed">
                {t('game.content.scoreboard_note')}
              </Text>
            </Stack>
          </Box>
        </Box>
        <Group justify="space-between">
          <Text size="sm" c="dimmed">
            {t('game.content.scoreboard_tip')}
          </Text>
          <Pagination
            value={activePage}
            onChange={setPage}
            total={Math.ceil((filteredList?.length ?? 1) / ITEM_COUNT_PER_PAGE)}
            boundaries={2}
          />
        </Group>
      </Stack>
      <ScoreboardItemModal
        scoreboard={scoreboard}
        bloodBonusMap={bloodData}
        opened={itemDetailOpened}
        withCloseButton={false}
        size="45rem"
        onClose={() => setItemDetailOpened(false)}
        item={currentItem}
      />
    </Paper>
  )
}
