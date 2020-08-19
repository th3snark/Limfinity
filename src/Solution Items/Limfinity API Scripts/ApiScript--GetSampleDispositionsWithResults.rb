# This script is called by the PcrPlateTool using the API.
data = params[:data]

plate_name = data[:plate_name]

results = find_subjects(query: search_query(subject_type: 'Plate'){|qb|
  qb.and(
    qb.prop("name").eq(plate_name)
)}, limit:1)

plate = results.first
samples = plate['Samples']

columnCount = plate['384'] ? 24 : 12

a = []

samples.each do |s|
  st = s['Sample Type']

  if s['Root Sample'].present?
    rs = s['Root Sample']

    if ['NTC', 'PCT', 'NEC'].include?(st)
      root_sample = ({
        :name => rs.name
      })
    else
      if rs['Test Results'].present?
        tr = rs['Test Results'].first
        test_result = ({
          :name => tr.name,
          :result => tr['Result'],
          :result_info => tr['Result Info'],
          :date_tested => tr['Date Tested'],
          :rna_pcr_sample => tr['RNA-PCR Sample'].present? ? tr['RNA-PCR Sample'].name : nil,
        })
      end

      root_sample = ({
        :name => rs.name,
        :last_rna_pcr_sample => rs['Last RNA-PCR Sample'].present? ? rs['Last RNA-PCR Sample'].name : nil,
        :test_result => test_result,
      })
    end
  end

  idx = s['Index'].to_i
  r_idx = idx / columnCount
  c_idx = idx % columnCount
  r = (r_idx + 65).chr
  c = c_idx + 1

  a << ({
    :index => idx,
    :position => "#{r}#{c}",
    :name => s.name,
    :sample_type => st,
    :terminated => s.terminated?,
    :canceled => s['Canceled'],
    :root_sample => root_sample,
  })

end

a.sort_by! { |x| x[:index] }

{
  :data => data,
  :result => ({
    :plate => ({
      :name => plate.name,
      :plate_type => plate['Plate Type'],
      :terminated => plate.terminated?,
      :canceled => plate['Canceled'],
      :sample_count => samples.count,
      :column_count => columnCount,
    }),
    :samples => a,
  })
}